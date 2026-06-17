using System;
using UnityEngine;

public static class AnimatorUtils
{
    public static void PlayerIfNotRunning(this Animator source, int fullPathHash, int layer = 0)
    {
        AnimatorStateInfo stateInfor = source.GetCurrentAnimatorStateInfo(layer);
        if (stateInfor.fullPathHash != fullPathHash)
        {
            source.Play(fullPathHash, layer);
        }
    }
    public static bool IsRunning(this Animator source, int fullPathHash, int layer = 0, float offset = 0)
    {
        AnimatorStateInfo stateInfo = source.GetCurrentAnimatorStateInfo(layer);
        return stateInfo.fullPathHash == fullPathHash && stateInfo.normalizedTime <= 1f - offset;
    }
    public static bool IsFinish(this Animator source, int fullPathHash, int layer = 0, float offset = 0)
    {
        AnimatorStateInfo stateInfo = source.GetCurrentAnimatorStateInfo(layer);
        return stateInfo.fullPathHash == fullPathHash && stateInfo.normalizedTime >=   1f - offset;
    }
    public static void PlayerIfNotRunning(this Animator source, string stateName, int layer = 0)
    {
        if (string.IsNullOrEmpty(stateName))
        {
            throw new ArgumentNullException("string null");
        }
        AnimatorStateInfo stateInfor = source.GetCurrentAnimatorStateInfo(layer);
        if (stateInfor.IsName(stateName))
        {
            source.Play(stateName, layer);
        }
    }
    public static bool IsRunning(this Animator source, string stateName, int layer = 0, float offset = 0)
    {      
        AnimatorStateInfo stateInfo = source.GetCurrentAnimatorStateInfo(layer);
        return stateInfo.IsName(stateName) && stateInfo.normalizedTime <= 1f - offset;
    }
    public static bool IsFinish(this Animator source, string stateName, int layer = 0, float offset = 0)
    {
        AnimatorStateInfo stateInfo = source.GetCurrentAnimatorStateInfo(layer);
        return stateInfo.IsName(stateName) && stateInfo.normalizedTime >= 1f - offset;
    }
}
