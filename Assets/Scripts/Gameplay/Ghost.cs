using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using DG.Tweening;

public class Ghost : MonoBehaviourPunCallbacks
{
    PlayerController playerController;
    public List<GameObject> meshToHide = new List<GameObject>();
    public List<SkinnedMeshRenderer> skinMeshMatToControl = new List<SkinnedMeshRenderer>();
    public List<MeshRenderer> meshMatToControl = new List<MeshRenderer>();
    bool isAttacking = false;
    [SerializeField] bool canAttacking;
    [SerializeField] float attackCooldown = 5f;
    float attackTimer;
    [SerializeField] bool isInvisible;
    public GameObject caughtCollider;
    [Header("Capture Human")]
    public Collider[] humanInRadiusList;
    [SerializeField] float nearbyDetectDistance = 2f;
    public LayerMask humanLayermask;
    [Header("Health Related")]
    [SerializeField] int hp = 100;
    public bool isDead;
    bool hpIsDecreasing, isHearMusic, doneCaptureAction;



    void Awake(){
        playerController = GetComponent<PlayerController>();
    }
    
    void Start()
    {
        if(photonView.IsMine){
            foreach(var mesh in meshToHide){
                mesh.SetActive(false); // local
            }

            /* if(!isInvisible){
                photonView.RPC("SetInvisible", RpcTarget.Others, true);
            } */
        }
    }

    [PunRPC]
    public void SetInvisible(bool invisible){
        if(invisible){
            isInvisible = true;
            //playerController.playerMesh.SetActive(false);
            
            foreach(var mesh in skinMeshMatToControl){  // Skinned Mesh Material
                if(mesh.GetComponent<SkinnedMeshRenderer>() != null){
                    foreach(var mat in mesh.GetComponent<SkinnedMeshRenderer>().materials){
                        mat.DOFloat(1f, "_SliderDissolve", 1f);
                    }
                }
            }

            foreach(var mesh2 in meshMatToControl){ // Mesh Material
                if(mesh2.GetComponent<MeshRenderer>() != null){
                    foreach(var mat in mesh2.GetComponent<MeshRenderer>().materials){
                        mat.DOFloat(1f, "_SliderDissolve", 1f);
                    }
                }
            }

        }else{
            isInvisible = false;
            //playerController.playerMesh.SetActive(true);

            foreach(var mesh in skinMeshMatToControl){ // Skinned Mesh Material
                if(mesh.GetComponent<SkinnedMeshRenderer>() != null){
                    foreach(var mat in mesh.GetComponent<SkinnedMeshRenderer>().materials){
                        mat.DOFloat(0f, "_SliderDissolve", 1f);
                    }
                }
            }

            foreach(var mesh2 in meshMatToControl){ // Mesh Material
                if(mesh2.GetComponent<MeshRenderer>() != null){
                    foreach(var mat in mesh2.GetComponent<MeshRenderer>().materials){
                        mat.DOFloat(0f, "_SliderDissolve", 1f);
                    }
                }
            }

        }
    }

    void Update(){
        if(!photonView.IsMine){
            return;
        }

        if(Input.GetMouseButtonDown(0) && canAttacking){
            if(!isAttacking){
                StartCoroutine(Attack());
            }
        }

        if(Input.GetMouseButtonDown(1)){
            playerController.anim.SetTrigger("Capture");
        }

        if(attackTimer > 0){
            attackTimer -= Time.deltaTime;
        }else{
            attackTimer = 0;
            canAttacking = true;
        }

        // ------------------------------------- DETECT HUMAN IN RANGE UPDATE START -----------------------------------------
        humanInRadiusList = Physics.OverlapSphere(this.transform.position, nearbyDetectDistance, humanLayermask);
        if(humanInRadiusList.Length > 0 && !GameManager.instance.gameEnded){

            // IF HUMAN FEAR LVL > 100, READY TO CAPTURE
            if(humanInRadiusList[0].gameObject.GetComponent<Human>().fearLevel >= 100 && !humanInRadiusList[0].gameObject.GetComponent<Human>().isCaptured){
                if(!doneCaptureAction){
                    GetComponent<PlayerUI>().captureTextUI.SetActive(true);
                }
                
                if(Input.GetButtonDown("Interact") && humanInRadiusList[0] != null && !humanInRadiusList[0].gameObject.GetComponent<Human>().isCaptured){ // press E if human fearlevel 100, caught the 1st on list
                    humanInRadiusList[0].gameObject.GetComponent<Human>().photonView.RPC("Captured", humanInRadiusList[0].gameObject.GetPhotonView().Owner);

                    playerController.anim.SetTrigger("Capture");

                    if(GetComponent<PlayerUI>().captureTextUI.activeSelf){
                        print("Close it");
                        GetComponent<PlayerUI>().captureTextUI.SetActive(false);
                    }

                    StartCoroutine(DoneCaptureReset());
                }
            }

        }else{
            if(GetComponent<PlayerUI>().captureTextUI.activeSelf){
                GetComponent<PlayerUI>().captureTextUI.SetActive(false);
            }
        }
        
        // ------------------------------------- DETECT HUMAN IN RANGE UPDATE END -----------------------------------------
    }

    IEnumerator DoneCaptureReset(){
        doneCaptureAction = true;
        yield return new WaitForSeconds(1f);
        doneCaptureAction = false;
    }

    IEnumerator HPDrainedOvertime(){
        hpIsDecreasing = true;
        while(hp <= 100 && hpIsDecreasing){
            yield return new WaitForSeconds(1f);
            photonView.RPC("AdjustHp", RpcTarget.All, -1);
        }
    }

    [PunRPC]
    public void EnableHpDrained(bool drain){
        if(drain){
            isHearMusic = true;
            StartCoroutine(HPDrainedOvertime());
        }else{
            isHearMusic = false;
            hpIsDecreasing = false;
        }
        
    }

    void OnDrawGizmos() {
        Gizmos.DrawWireSphere(this.transform.position, nearbyDetectDistance);
    }

    IEnumerator Attack(){
        isAttacking = true;
        canAttacking = false;
        attackTimer = attackCooldown;
        //photonView.RPC("SetInvisible", RpcTarget.Others, false);
        playerController.anim.SetTrigger("Attack/Jumpscare");
        yield return new WaitForSeconds(1f);
        isAttacking = false;

        // Hide Mesh
        yield return new WaitForSeconds(3f);
        /* if(!isInvisible){
            photonView.RPC("SetInvisible", RpcTarget.Others, true);
        } */
    }

    public void EnableCaughtCollider(){
        caughtCollider.SetActive(true);
    }

    public void DisableCaughtCollider(){
        caughtCollider.SetActive(false);
    }

    // ---------------------------------------------- GHOST HP RELATED START ---------------------
    [PunRPC]
    public void AdjustHp(int amount){
        hp += amount;

        if(hp <= 0){
            hp = 0;
            isDead = true;
            // Respawned
            photonView.RPC("Respawned", RpcTarget.All);
        }else if(hp >= 100){
            hp = 100;
        }
    } // end AdjustHp()

    [PunRPC]
    public IEnumerator Respawned(){
        if(photonView.IsMine){
            yield return new WaitForSeconds(1f);
            // Transfer to prison
            int randomNmbr = Random.Range(0, GameManager.instance.spawnpoints_Ghost.Count);
            playerController.canMove = false; // false to make player move to new position
            playerController.StopMovement(); // Stop Completely
            //transform.position = GameManager.instance.spawnpoints_Ghost[randomNmbr].position;
            transform.position = Vector3.zero;
            yield return new WaitForSeconds(3f);
            playerController.UnstopMovement();
            isDead = false;
            photonView.RPC("AdjustHp", RpcTarget.All, 100); // Reset HP to 100
        }
    }
    // ---------------------------------------------- GHOST HP RELATED END ---------------------
}
