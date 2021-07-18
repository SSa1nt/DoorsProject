using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

public enum RumblePattern
{
    Constant,
    Pulse,
    Linear
}

public class GamepadRumbler : MonoBehaviour
{
    public static GamepadRumbler Instance { get; private set; } = null;

    //private PlayerInput _playerInput;
    Gamepad gamepad;
    private RumblePattern activeRumbePattern;
    private float rumbleDurration;
    private float pulseDurration;
    private float lowA;
    private float lowStep;
    private float highA;
    private float highStep;
    private float rumbleStep;
    private bool isMotorActive = false;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        SetGamepad();

    }

    // Public Methods

    public void SetGamepad()
    {
        gamepad = GetGamepad();
    }

    public void RumbleConstant(float low, float high, float durration)
    {
        activeRumbePattern = RumblePattern.Constant;
        lowA = low;
        highA = high;
        rumbleDurration = Time.time + durration;
    }

    public void RumblePulse(float low, float high, float burstTime, float durration)
    {
        activeRumbePattern = RumblePattern.Pulse;
        lowA = low;
        highA = high;
        rumbleStep = burstTime;
        pulseDurration = Time.time + burstTime;
        rumbleDurration = Time.time + durration;
        isMotorActive = true;
        gamepad?.SetMotorSpeeds(lowA, highA);
    }

    public void RumbleLinear(float lowStart, float lowEnd, float highStart, float highEnd, float durration)
    {
        activeRumbePattern = RumblePattern.Linear;
        lowA = lowStart;
        highA = highStart;
        lowStep = (lowEnd - lowStart) / durration;
        highStep = (highEnd - highStart) / durration;
        rumbleDurration = Time.time + durration;
    }

    public void StopRumble()
    {
        if (gamepad != null)
        {
            gamepad.SetMotorSpeeds(0, 0);
        }
    }

    private void Update()
    {
        if (Time.time > rumbleDurration)
        {
            StopRumble();
            return;
        }

        if (gamepad == null) return;

        switch (activeRumbePattern)
        {
            case RumblePattern.Constant:
                gamepad.SetMotorSpeeds(lowA, highA);
                break;

            case RumblePattern.Pulse:

                if (Time.time > pulseDurration)
                {
                    isMotorActive = !isMotorActive;
                    pulseDurration = Time.time + rumbleStep;
                    if (!isMotorActive)
                    {
                        gamepad.SetMotorSpeeds(0, 0);
                    }
                    else
                    {
                        gamepad.SetMotorSpeeds(lowA, highA);
                    }
                }

                break;
            case RumblePattern.Linear:
                gamepad.SetMotorSpeeds(lowA, highA);
                lowA += (lowStep * Time.deltaTime);
                highA += (highStep * Time.deltaTime);
                break;
            default:
                break;
        }
    }

    public void RumbleDamaged(float dmg, float currentHealth, float t)
    {
        RumbleLinear(dmg, dmg, dmg, currentHealth, t);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        StopRumble();
    }

    // Private helpers

    private Gamepad GetGamepad()
    {
        return Gamepad.current;
        //return Gamepad.all.FirstOrDefault(g => _playerInput.devices.Any(d => d.deviceId == g.deviceId));
    }
}
