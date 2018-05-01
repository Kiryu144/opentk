//
// The Open Toolkit Library License
//
// Copyright (c) 2006 - 2013 Stefanos Apostolopoulos for the Open Toolkit library.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Input;

namespace OpenTK.Platform.SDL2
{
    internal class Sdl2InputDriver : IInputDriver2
    {
        private readonly static Dictionary<IntPtr, Sdl2InputDriver> DriverHandles =
            new Dictionary<IntPtr, Sdl2InputDriver>();

        private readonly IntPtr driver_handle;

        private readonly Sdl2Keyboard keyboard_driver = new Sdl2Keyboard();
        private readonly Sdl2Mouse mouse_driver = new Sdl2Mouse();
        private readonly Sdl2JoystickDriver joystick_driver = new Sdl2JoystickDriver();

        private readonly EventFilter EventFilterDelegate_GCUnsafe = FilterInputEvents;
        private readonly IntPtr EventFilterDelegate;

        private static int count;
        private bool disposed;

        public Sdl2InputDriver()
        {
            lock (SDL.Sync)
            {
                SDL.GameControllerEventState(EventState.Enable);
                SDL.JoystickEventState(EventState.Enable);

                EventFilterDelegate = Marshal.GetFunctionPointerForDelegate(EventFilterDelegate_GCUnsafe);
                driver_handle = new IntPtr(count++);
                DriverHandles.Add(driver_handle, this);
                SDL.AddEventWatch(EventFilterDelegate, driver_handle);
                if (SDL.InitSubSystem(SystemFlags.JOYSTICK) < 0)
                {
                    Debug.Print("[SDL2] InputDriver failed to init Joystick subsystem. Error: {0}", SDL.GetError());
                }
                if (SDL.InitSubSystem(SystemFlags.GAMECONTROLLER) < 0)
                {
                    Debug.Print("[SDL2] InputDriver failed to init GameController subsystem. Error: {0}", SDL.GetError());
                }
            }
        }

        private unsafe static int FilterInputEvents(IntPtr driver_handle, IntPtr e)
        {
            try
            {
                Event ev = *(Event*)e;

                Sdl2InputDriver driver;
                if (DriverHandles.TryGetValue(driver_handle, out driver))
                {
                    switch (ev.Type)
                    {
                        case EventType.KEYDOWN:
                        case EventType.KEYUP:
                            driver.keyboard_driver.ProcessKeyboardEvent(ev.Key);
                            break;

                        case EventType.MOUSEBUTTONDOWN:
                        case EventType.MOUSEBUTTONUP:
                            driver.mouse_driver.ProcessMouseEvent(ev.Button);
                            break;

                        case EventType.MOUSEMOTION:
                            driver.mouse_driver.ProcessMouseEvent(ev.Motion);
                            break;

                        case EventType.MOUSEWHEEL:
                            driver.mouse_driver.ProcessWheelEvent(ev.Wheel);
                            break;

                        case EventType.JOYDEVICEADDED:
                        case EventType.JOYDEVICEREMOVED:
                            driver.joystick_driver.ProcessJoystickEvent(ev.JoyDevice);
                            break;

                        case EventType.JOYAXISMOTION:
                            driver.joystick_driver.ProcessJoystickEvent(ev.JoyAxis);
                            break;

                        case EventType.JOYBALLMOTION:
                            driver.joystick_driver.ProcessJoystickEvent(ev.JoyBall);
                            break;

                        case EventType.JOYBUTTONDOWN:
                        case EventType.JOYBUTTONUP:
                            driver.joystick_driver.ProcessJoystickEvent(ev.JoyButton);
                            break;

                        case EventType.JOYHATMOTION:
                            driver.joystick_driver.ProcessJoystickEvent(ev.JoyHat);
                            break;

#if USE_SDL2_GAMECONTROLLER
                        case EventType.CONTROLLERDEVICEADDED:
                        case EventType.CONTROLLERDEVICEREMOVED:
                            driver.joystick_driver.ProcessControllerEvent(ev.ControllerDevice);
                            break;

                        case EventType.CONTROLLERAXISMOTION:
                            driver.joystick_driver.ProcessControllerEvent(ev.ControllerAxis);
                            break;

                        case EventType.CONTROLLERBUTTONDOWN:
                        case EventType.CONTROLLERBUTTONUP:
                            driver.joystick_driver.ProcessControllerEvent(ev.ControllerButton);
                            break;
#endif
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }

            return 0;
        }

        public IMouseDriver2 MouseDriver => mouse_driver;

        public IKeyboardDriver2 KeyboardDriver => keyboard_driver;

        public IGamePadDriver GamePadDriver => joystick_driver;

        public IJoystickDriver2 JoystickDriver => joystick_driver;

        private void Dispose(bool manual)
        {
            if (!disposed)
            {
                if (manual)
                {
                    Debug.Print("Disposing {0}", GetType());
                    joystick_driver.Dispose();
                    lock (SDL.Sync)
                    {
                        SDL.DelEventWatch(EventFilterDelegate, driver_handle);
                    }
                    DriverHandles.Remove(driver_handle);
                }
                else
                {
                    Debug.WriteLine("Sdl2InputDriver leaked, did you forget to call Dispose()?");
                }
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Sdl2InputDriver()
        {
            Dispose(false);
        }
    }
}

