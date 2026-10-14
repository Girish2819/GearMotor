using UnityEngine;

public class GearRotate : MonoBehaviour
{
    public float gearRatio = 1f;
    public bool reverseDirection;
    public MotorController motor;

    void Update()
    {
        if (motor == null || !motor.motorOn) return;

        float rpm = motor.rpm * gearRatio;
        float dps = rpm * 6f;
        float dir = reverseDirection ? -1f : 1f;

        transform.Rotate(Vector3.forward * dir * dps * Time.deltaTime, Space.Self);
    }
}