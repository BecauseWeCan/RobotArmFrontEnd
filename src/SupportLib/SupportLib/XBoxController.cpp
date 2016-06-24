#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <XInput.h>
#include "stdafx.h"
#include <malloc.h>
#include <stdlib.h>
#include <stdio.h>
#include "XBoxController.h"

namespace SupportLib {
	public enum class Button
	{
		DPAD_UP,
		DPAD_DOWN,
		DPAD_LEFT,
		DPAD_RIGHT,
		START,
		BACK,
		LEFT_THUMB,
		RIGHT_THUMB,
		LEFT_SHOULDER,
		RIGHT_SHOULDER,
		A,
		B,
		X,
		Y,
	};

	public enum class Thumbstick
	{
		LEFT_X,
		LEFT_Y,
		RIGHT_X,
		RIGHT_Y,
	};

	public enum class Trigger
	{
		LEFT,
		RIGHT,
	};
	public ref class XBoxController
	{
	private:
		XINPUT_STATE *ControllerState;
		int ControllerNumber;
	public:
		XBoxController(int controller_number)
		{
			ControllerNumber = controller_number;
			ControllerState = (XINPUT_STATE *)malloc(sizeof(XINPUT_STATE));
		}

		~XBoxController()
		{
			free(ControllerState);
		}

		bool IsConnected()
		{
			ZeroMemory(ControllerState, sizeof(XINPUT_STATE));
			DWORD Result = XInputGetState(ControllerNumber, ControllerState);
			return (Result == ERROR_SUCCESS);
		}
		int PacketNumber()
		{
			return ControllerState->dwPacketNumber;
		}

		bool Button(Button ID)
		{
			WORD button = ControllerState->Gamepad.wButtons;
			switch (ID)
			{
			case Button::DPAD_UP:
				return (button & XINPUT_GAMEPAD_DPAD_UP) != 0;
			case Button::DPAD_DOWN:
				return (button & XINPUT_GAMEPAD_DPAD_DOWN) != 0;
			case Button::DPAD_LEFT:
				return (button & XINPUT_GAMEPAD_DPAD_LEFT) != 0;
			case Button::DPAD_RIGHT:
				return (button & XINPUT_GAMEPAD_DPAD_RIGHT) != 0;
			case Button::START:
				return (button & XINPUT_GAMEPAD_START) != 0;
			case Button::BACK:
				return (button & XINPUT_GAMEPAD_BACK) != 0;
			case Button::LEFT_THUMB:
				return (button & XINPUT_GAMEPAD_LEFT_THUMB) != 0;
			case Button::RIGHT_THUMB:
				return (button & XINPUT_GAMEPAD_RIGHT_THUMB) != 0;
			case Button::LEFT_SHOULDER:
				return (button & XINPUT_GAMEPAD_LEFT_SHOULDER) != 0;
			case Button::RIGHT_SHOULDER:
				return (button & XINPUT_GAMEPAD_RIGHT_SHOULDER) != 0;
			case Button::A:
				return (button & XINPUT_GAMEPAD_A) != 0;
			case Button::B:
				return (button & XINPUT_GAMEPAD_B) != 0;
			case Button::X:
				return (button & XINPUT_GAMEPAD_X) != 0;
			case Button::Y:
				return (button & XINPUT_GAMEPAD_Y) != 0;
			default:
				break;
			}
			return false;
		}

		double Thumbstick(Thumbstick ID)
		{
			double stick_value = 0.0;
			switch (ID)
			{
				case Thumbstick::LEFT_X:
				{
					stick_value = (double)(ControllerState->Gamepad.sThumbLX) / 32768.0;
					break;
				}
				case Thumbstick::LEFT_Y:
				{
					stick_value = (double)(ControllerState->Gamepad.sThumbLY) / 32768.0;
					break;
				}
				case Thumbstick::RIGHT_X:
				{
					stick_value = (double)(ControllerState->Gamepad.sThumbRX) / 32768.0;
					break;
				}
				case Thumbstick::RIGHT_Y:
				{
					stick_value = (double)(ControllerState->Gamepad.sThumbRY) / 32768.0;
					break;
				}
				default:
					break;
			}
			if (stick_value > DEAD_BAND)
			{
				stick_value -= DEAD_BAND;
			}
			else if (stick_value < -DEAD_BAND)
			{
				stick_value += DEAD_BAND;
			}
			else
			{
				stick_value = 0.0;
			}

			return stick_value;
		}

		double Trigger(Trigger ID)
		{
			switch (ID)
			{
			case Trigger::LEFT:
				return (double)(ControllerState->Gamepad.bLeftTrigger) / 256.0;
			case Trigger::RIGHT:
				return (double)(ControllerState->Gamepad.bRightTrigger) / 256.0;
			default:
				break;
			}
			return 0.0;
		}

		void Vibrate(int leftVal, int rightVal)
		{
			XINPUT_VIBRATION Vibration;
			Vibration.wLeftMotorSpeed = leftVal;
			Vibration.wRightMotorSpeed = rightVal;
			XInputSetState(ControllerNumber, &Vibration);
		}
	};
}


