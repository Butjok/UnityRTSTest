using System.Collections;
using UnityEngine;

public class PlayerCameraManager : WorldBehaviour {

    public PlayerController playerController;
    public Vector3 position;
    public Vector3 startPositionOffset = new(0, 10, -10);
    public float yaw;
    public float pitch = 45;
    public float rotationYawStepDegrees = 45;
    public float rotationDuration = .25f;
    public Easing.Type rotationEasing = Easing.Type.InOutQuadratic;

    public IEnumerator rotationCoroutine;

    public void Awake() {
        position = playerController.transform.position + startPositionOffset;
    }

    public void GetView(out Vector3 position, out Quaternion rotation) {
        position = this.position;
        rotation = Quaternion.Euler(pitch, yaw, 0);
    }

    public bool TryRotateCamera(int direction) {
        if (rotationCoroutine != null)
            return false;
        rotationCoroutine = RotateCamera(direction);
        StartCoroutine(rotationCoroutine);
        return true;
    }

    public IEnumerator RotateCamera(int direction) {
        var centerRay = new Ray(position, Quaternion.Euler(pitch, yaw, 0) * Vector3.forward);
        if (Physics.Raycast(centerRay, out var hitInfo, 100)) {
            var pivot = hitInfo.point;
            var startOffsetWS = position - pivot;
            var startTime = Time.time;
            var lastOffsetWS = startOffsetWS;
            var lastYaw = 0f;
            while (Time.time < startTime + rotationDuration) {
                var t = (Time.time - startTime) / rotationDuration;
                t = Easing.Evaluate(rotationEasing, t);
                var yaw = direction * rotationYawStepDegrees * t;
                var offsetWS = Quaternion.Euler(0, yaw, 0) * startOffsetWS;
                position += offsetWS - lastOffsetWS;
                this.yaw += yaw - lastYaw;
                lastYaw = yaw;
                lastOffsetWS = offsetWS;
                yield return null;
            }
            // snap yaw to step degrees
            this.yaw = Mathf.Round(this.yaw / rotationYawStepDegrees) * rotationYawStepDegrees;
        }
        rotationCoroutine = null;
    }
}