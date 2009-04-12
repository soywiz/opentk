﻿using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Input;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;

namespace OpenTK.Platform.Windows
{
    // This driver supports asynchronous keyboard, mouse and joystick input (the latter via WinMM).
    // Only a single keyboard and mouse is supported.
    sealed class WinAsyncInput : IInputDriver
    {
        #region Fields

        readonly IList<KeyboardDevice> keyboard_list;
        readonly IList<MouseDevice> mouse_list;
        readonly WinMMJoystick joystick_driver = new WinMMJoystick();

        readonly byte[] key_states = new byte[256];
        readonly WinKeyMap key_map = new WinKeyMap();

        bool disposed;

        #endregion

        #region Constructors

        public WinAsyncInput()
        {
            KeyboardDevice keyboard = new KeyboardDevice();
            keyboard.Description = "Default Windows keyboard.";
            List<KeyboardDevice> klist = new List<KeyboardDevice>();
            klist.Add(keyboard);
            keyboard_list = klist.AsReadOnly();

            MouseDevice mouse = new MouseDevice();
            keyboard.Description = "Default Windows mouse.";
            List<MouseDevice> mlist = new List<MouseDevice>();
            mlist.Add(mouse);
            mouse_list = mlist.AsReadOnly();
        }

        #endregion

        #region IInputDriver Members

        public void Poll()
        {
            joystick_driver.Poll();

            UnsafeNativeMethods.GetKeyboardState(key_states);
            for (int i = 0; i < key_states.Length; i++)
            {
                bool pressed = (key_states[i] & 0xA0) != 0;
                bool toggled = (key_states[i] & 0x01) != 0;
                VirtualKeys vkey = (VirtualKeys)i;
                switch (vkey)
                {
                    case VirtualKeys.CAPITAL:
                    case VirtualKeys.NUMLOCK:
                        // Todo: which other toggle keys are there?
                        Keyboard[0][key_map[vkey]] = pressed ^ toggled;
                        break;

                    case VirtualKeys.LBUTTON: Mouse[0][MouseButton.Left] = pressed; break;
                    case VirtualKeys.MBUTTON: Mouse[0][MouseButton.Middle] = pressed; break;
                    case VirtualKeys.RBUTTON: Mouse[0][MouseButton.Right] = pressed; break;
                    case VirtualKeys.XBUTTON1: Mouse[0][MouseButton.Button1] = pressed; break;
                    case VirtualKeys.XBUTTON2: Mouse[0][MouseButton.Button2] = pressed; break;

                    default: Keyboard[0][key_map[vkey]] = pressed; break;
                }
            }
        }

        #endregion

        #region IKeyboardDriver Members

        public IList<KeyboardDevice> Keyboard
        {
            get { return keyboard_list; }
        }

        #endregion

        #region IMouseDriver Members

        public IList<MouseDevice> Mouse
        {
            get { return mouse_list; }
        }

        #endregion

        #region IJoystickDriver Members

        public IList<JoystickDevice> Joysticks
        {
            get { return joystick_driver.Joysticks; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool manual)
        {
            if (!disposed)
            {
                if (manual)
                {
                    joystick_driver.Dispose();
                }
                disposed = true;
            }
        }

        #endregion

        #region UnsafeNativeMethods

        static class UnsafeNativeMethods
        {
            [DllImport("user32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
            unsafe static extern bool GetKeyboardState(byte* keyState);

            public static void GetKeyboardState(byte[] state)
            {
                if (state.Length < 256)
                    throw new ArgumentException("state");

                unsafe
                {
                    fixed (byte* state_ptr = &state[0])
                    {
                        if (!GetKeyboardState(state_ptr))
                            Debug.Print("Error {0} while retrieving keyboard state.", Marshal.GetLastWin32Error());
                    }
                }
            }
        }

        #endregion
    }
}
