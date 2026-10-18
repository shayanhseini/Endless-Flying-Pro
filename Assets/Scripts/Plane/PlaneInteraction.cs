using UnityEngine;

public class PlaneInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShieldController shield;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameAudioController gameAudioController;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffect;
    
    public void HitObstacle()
    {
        GameEvents.RaisePlaneHit();

        PlayHitEffect();
        gameAudioController.PlayExplosionSound(this.transform);

        if (shield != null && shield.IsActive)
        {
            shield.Consume();
            return;
        }
        
        DestroyPlane();
    }


    private void PlayHitEffect()
    {
        if (hitEffect == null)
            return;

        hitEffect.SetActive(false);

        hitEffect.transform.position =
            transform.position;

        hitEffect.SetActive(true);
    }

    

    private void DestroyPlane()
    {

        gameManager.GameOver();
    }
}
