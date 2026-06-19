using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditor.Animations;

public class MotionTest : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private Animator _targetAnim;
    private List<string> _stateNames = new List<string>();
    private int _targetClipNum = 0;
    [SerializeField] TextMeshProUGUI _targetClipName;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(_targetAnim.runtimeAnimatorController);
        if (_targetAnim.runtimeAnimatorController.GetType() == typeof(AnimatorOverrideController))
        {
            AnimatorOverrideController overrideCtrl = (AnimatorOverrideController)_targetAnim.runtimeAnimatorController;
            AnimatorController animCtrl = (AnimatorController)overrideCtrl.runtimeAnimatorController;
            if (animCtrl != null)
            {
                AnimatorControllerLayer animLayer = animCtrl.layers[0];
                foreach (var state in animLayer.stateMachine.states)
                {
                    AnimatorState animState = state.state;
                    _stateNames.Add(animState.name);
                }
            }
        }
        else
        {
            AnimatorController animCtrl = (AnimatorController)_targetAnim.runtimeAnimatorController;
            if (animCtrl != null)
            {
                AnimatorControllerLayer animLayer = animCtrl.layers[0];
                foreach (var state in animLayer.stateMachine.states)
                {
                    AnimatorState animState = state.state;
                    _stateNames.Add(animState.name);
                }
            }
        }
        _targetClipName.text = _stateNames[_targetClipNum];
    }

    public void ChangeTargetClip(int change)
    {
        _targetClipNum = (int)Mathf.Repeat(_targetClipNum + change, _stateNames.Count);
        _targetClipName.text = _stateNames[_targetClipNum];
        PlayTargetClip();
    }

    public void PlayTargetClip()
    {
        _targetAnim.Play(_stateNames[_targetClipNum], 0);
        if(_stateNames[_targetClipNum] == "Move"|| _stateNames[_targetClipNum] == "Move_L" || _stateNames[_targetClipNum] == "Move_R")
        {
            _targetAnim.SetBool("IsMove",true);
        }
        else
        {
            _targetAnim.SetBool("IsMove", false);
        }
    }
#endif
}
