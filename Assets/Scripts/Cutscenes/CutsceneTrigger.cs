using UnityEngine;
using Project.Visuals;

public class CutsceneTrigger : MonoBehaviour
{
    [SerializeField] private CutsceneController cutscene;
    [SerializeField] private bool triggerOnce = true;

    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        if (triggerOnce) triggered = true;
        cutscene.Play();
    }
}