using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Photon.Pun;
using Photon.Realtime;

public class Interact_Door : MonoBehaviourPunCallbacks
{
    List<GameObject> capturedHumanList = new List<GameObject>();
    public bool openOutward;
    public bool isOpened;
    bool actionInProgress;
    public bool humanInside;
    // Button Related
    E_ButtonType originalBtnType;
    float originalHoldDuration;
    public AK.Wwise.Event openSound, closeSound;


    public void OpenDoor(){
        if(!actionInProgress){
            if(NetworkManager.instance != null){
                photonView.RPC("HandleDoor", RpcTarget.AllBuffered);
            }else{
                HandleDoor();
            }
            actionInProgress = true;
        }
    }

    [PunRPC]
    void HandleDoor(){
        if(!isOpened){
            // Play sound
            openSound.Post(gameObject);
            /*if(GetComponent<AudioSource>().clip != null){
                GetComponent<AudioSource>().Play();
            }*/

            if(!openOutward){
                transform.DORotateQuaternion(new Quaternion(-0.5f,-0.5f,-0.5f,0.5f), 1.5f).OnComplete(() => { isOpened = true; actionInProgress = false; } );
            }else{
                transform.DORotateQuaternion(new Quaternion(-0.5f,0.5f,0.5f,0.5f), 1.5f).OnComplete(() => { isOpened = true; actionInProgress = false;} );
            }

            if(capturedHumanList.Count > 0){
                // Set captured human to Uncaptured
                foreach(var human in capturedHumanList){
                    if(!human.GetComponent<Human>().isDead){
                        human.GetComponent<Human>().photonView.RPC("Released", human.GetPhotonView().Owner);
                    }
                }

                GetComponent<Interactable>().buttonType = originalBtnType;
                GetComponent<Interactable>().holdDuration = originalHoldDuration;

                if(humanInside){
                    humanInside = false;
                }
            }
        }else{
            closeSound.Post(gameObject);
            transform.DORotateQuaternion(new Quaternion(-0.707106829f,0,0,0.707106829f), 1.5f).OnComplete(() => { isOpened = false; actionInProgress = false; } );
        }
    }

    [PunRPC]
    public void HumanInsideRoom(int playerViewID){
        var player = PhotonView.Find(playerViewID).gameObject;
        capturedHumanList.Add(player);
        print("Added " + player + " inside prison");

        // Store original value
        originalBtnType = GetComponent<Interactable>().buttonType;
        originalHoldDuration = GetComponent<Interactable>().holdDuration;

        // Change to new value
        GetComponent<Interactable>().buttonType = E_ButtonType.HOLD;
        GetComponent<Interactable>().holdDuration = 3f;
        if(isOpened){
            isOpened = false;
            transform.DORotateQuaternion(new Quaternion(-0.707106829f,0,0,0.707106829f), 1f);
        }

        if(!humanInside){
            humanInside = true;
        }
    }

}
