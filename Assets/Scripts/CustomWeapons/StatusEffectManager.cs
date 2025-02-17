using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    public GameObject energizedEffect;
    public bool isEnergized;

    /*
    public void StartEnergizedEffect(float duration)
    {
        isEnergized = true;
        energizedEffect.SetActive(true);
        energizedEffect.transform.Find("RadialProgressBar").GetComponent<CircularProgressBar>().ActivateCountdown(duration);
        StartCoroutine(EndEnergizedEffect(duration));
    }


    IEnumerator EndEnergizedEffect(float delay)
    {
        yield return new WaitForSeconds(delay);

        isEnergized = false;
        energizedEffect.SetActive(false);
    }
    */
}
