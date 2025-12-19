using UnityEngine;

public class DeliveryZone : MonoBehaviour
{
    // Ziehe hier im Inspektor dein Haupt-Zug-Objekt rein (wo der TrainController drauf liegt)
    public TrainController trainController;

    private void OnTriggerStay(Collider other)
    {
        // Wir prüfen nur, ob es der Spieler ist
        if (other.CompareTag("Player"))
        {
            // Wir leiten den Spieler an den Zug weiter
            if (trainController != null)
            {
                trainController.AttemptDelivery(other.GetComponent<PlayerController>());
            }
        }
    }
}