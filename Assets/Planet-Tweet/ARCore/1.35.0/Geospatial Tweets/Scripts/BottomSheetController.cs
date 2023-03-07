using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class BottomSheetController : MonoBehaviour
{
    // Reference to the Panel object
    public GameObject bottomSheetView;

    // Reference to the Animator component
    private Animator animator;

    public Button MapButton;

    public AnimatorStateInfo state; 

    // Start is called before the first frame update
    void Start()
    {
        // Get the Animator component
        animator = bottomSheetView.GetComponent<Animator>();
        state = animator.GetCurrentAnimatorStateInfo(0);


    }

    // Method to toggle the bottom sheet
    public void OnSetBottomSheet()
    {
        if (state.IsName("isOpen")) {
            // The trigger is set
            animator.SetTrigger("isClose");
        } else {
            animator.SetTrigger("isOpen");
        }
    }

     // Method to toggle the bottom sheet
    public void OnCloseBottomSheet()
    {
        if (state.IsName("isClose")) {
            animator.SetTrigger("isOpen");
        } else {
            animator.SetTrigger("isClose");
        }
    }
}
