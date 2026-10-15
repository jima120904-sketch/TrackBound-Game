using UnityEngine;

public sealed class TrainInteractable : MonoBehaviour
{
    private TrainDrive _train;
    private Transform _hinge;
    private bool _isOpen;

    public string Hint => _isOpen ? "E  Feed one coal chunk" : "E  Open firebox door";

    public void Initialize(TrainDrive train, Transform hinge)
    {
        _train = train;
        _hinge = hinge;
    }

    public void Interact(PlayerMotor player)
    {
        if (!_isOpen)
        {
            _isOpen = true;
            _hinge.localRotation = Quaternion.Euler(-88f, 0, 0);
            player.SetMessage("Firebox door open");
            return;
        }

        _train.FeedFuel(player);
    }
}