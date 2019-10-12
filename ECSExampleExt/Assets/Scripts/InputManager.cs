using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : Singleton<InputManager>
{
    public enum InputMode
    {
        UIMode = 0,
        GameMode
    };

    public InputMode Mode = InputMode.GameMode;
    public CameraController Camera;
    public float CameraSpeed = 2f;

    void Start()
    {
        UIManager.instance.UpdateInputMode(Mode);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            Mode = (Mode == InputMode.UIMode) ? InputMode.GameMode : InputMode.UIMode;
            UIManager.instance.UpdateInputMode(Mode);
        }

        if (Input.GetKeyUp(KeyCode.Space))
            UIManager.instance.ShowHideUI();

        switch (Mode)
        {
            case InputMode.GameMode:
                {
                    float forwardValue = 0f;
                    float rightValue = 0f;

                    float yaw = CameraSpeed * Input.GetAxis("Mouse X");
                    float pitch = CameraSpeed * Input.GetAxis("Mouse Y");

                    if (Input.GetKey(KeyCode.W))
                        forwardValue += CameraSpeed;
                    else if (Input.GetKey(KeyCode.S))
                        forwardValue -= CameraSpeed;

                    if (Input.GetKey(KeyCode.D))
                        rightValue += CameraSpeed;
                    else if (Input.GetKey(KeyCode.A))
                        rightValue -= CameraSpeed;

                    Camera.MoveCam(forwardValue, rightValue);
                    Camera.RotateCam(yaw, pitch);
                }
                break;

            case InputMode.UIMode:
                {

                }
                break;
        }
    }
}
