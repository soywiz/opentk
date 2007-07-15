﻿#region --- License ---
/* Copyright (c) 2007 Stephen Apostolopoulos
 * See license.txt for license info
 */
#endregion

#region --- Using directives ---

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

#endregion

namespace OpenTK.Platform.Windows
{
    sealed class WinGLNative : NativeWindow, OpenTK.Platform.IGLWindow, IDisposable
    {
        private WinGLContext glContext;
        private OpenTK.Platform.DisplayMode mode;

        #region --- Contructors ---

        /// <summary>
        /// Constructs a new WinGLNative window, using safe defaults for the DisplayMode.
        /// </summary>
        public WinGLNative()
        {
            mode = new DisplayMode();
            mode.Width = 640;
            mode.Height = 480;

            this.CreateWindow(mode);
        }

        #endregion

        #region private void CreateWindow()

        private void CreateWindow(DisplayMode mode)
        {

            CreateParams cp = new CreateParams();
            cp.ClassStyle =
                (int)WinApi.WindowClassStyle.OwnDC |
                (int)WinApi.WindowClassStyle.VRedraw |
                (int)WinApi.WindowClassStyle.HRedraw;
            cp.Style =
                (int)WinApi.WindowStyle.Visible |
                (int)WinApi.WindowStyle.ClipChildren |
                (int)WinApi.WindowStyle.ClipSiblings |
                (int)WinApi.WindowStyle.OverlappedWindow;
            cp.Width = mode.Width;
            cp.Height = mode.Height;
            cp.Caption = "OpenTK Game Window";
            base.CreateHandle(cp);

            glContext = new WinGLContext(
                this.Handle,
                new ColorDepth(32),
                new ColorDepth(0),
                24,
                8,
                0,
                false,
                true
            );
        }

        /*
        private void CreateWindow()
        {
            WinApi.WindowClass wc = new WinApi.WindowClass();
            wc.style =
                WinApi.WindowClassStyle.HRedraw |
                WinApi.WindowClassStyle.VRedraw |
                WinApi.WindowClassStyle.OwnDC;
            wc.WindowProcedure = new WinApi.WindowProcedureEventHandler(WndProc);
            wc.Instance = instance;
            //wc.ClassName = Marshal.StringToHGlobalAuto(className);
            wc.ClassName = className;

            classAtom = WinApi.RegisterClass(wc);

            if (classAtom == 0)
            {
                throw new Exception("Could not register class, error: " + Marshal.GetLastWin32Error());
            }
            
            // Change for fullscreen!
            handle = WinApi.CreateWindowEx(
                WinApi.ExtendedWindowStyle.ApplicationWindow |
                WinApi.ExtendedWindowStyle.OverlappedWindow |
                WinApi.ExtendedWindowStyle.Topmost,
                className,
                //Marshal.StringToHGlobalAuto("OpenTK Game Window"),
                "OpenTK Game Window",
                WinApi.WindowStyle.OverlappedWindow |
                WinApi.WindowStyle.ClipChildren |
                WinApi.WindowStyle.ClipSiblings,
                0, 0,
                640, 480,
                IntPtr.Zero,
                IntPtr.Zero,
                instance,
                IntPtr.Zero
            );

            if (handle == IntPtr.Zero)
            {
                throw new Exception("Could not create window, error: " + Marshal.GetLastWin32Error());
            }
        }
        */
        #endregion

        #region protected override void WndProc(ref Message m)

        /// <summary>
        /// For use in WndProc only.
        /// </summary>
        private int width, height;

        /// <summary>
        /// Processes incoming WM_* messages.
        /// </summary>
        /// <param name="m">Reference to the incoming Windows Message.</param>
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WinApi.Constants.WM_WINDOWPOSCHANGED:
                    // Get window size
                    width = Marshal.ReadInt32(m.LParam, (int)Marshal.OffsetOf(typeof(WinApi.WindowPosition), "cx"));
                    height = Marshal.ReadInt32(m.LParam, (int)Marshal.OffsetOf(typeof(WinApi.WindowPosition), "cy"));
                    //if (resizeEventArgs.Width != width || resizeEventArgs.Height != height)
                    if (mode.Width != width || mode.Height != height)
                    {
                        // If the size has changed, raise the ResizeEvent.
                        resizeEventArgs.Width = width;
                        resizeEventArgs.Height = height;
                        this.OnResize(resizeEventArgs);
                        // The message was processed.
                        return;
                    }
                    // If the message was not a resize notification, send it to the default WndProc.
                    break;

                case WinApi.Constants.WM_CREATE:
                    // Set the window width and height:
                    mode.Width = Marshal.ReadInt32(m.LParam, (int)Marshal.OffsetOf(typeof(WinApi.CreateStruct), "cx"));
                    mode.Height = Marshal.ReadInt32(m.LParam, (int)Marshal.OffsetOf(typeof(WinApi.CreateStruct), "cy"));
                    
                    // Raise the Create event
                    this.OnCreate(EventArgs.Empty);

                    // Raise the resize event:
                    //resizeEventArgs.Width = width;
                    //resizeEventArgs.Height = height;
                    //this.OnResize(resizeEventArgs);
                    return;

                case WinApi.Constants.WM_KEYDOWN:
                case WinApi.Constants.WM_KEYUP:
                    this.ProcessKey(ref m);
                    return;
                
                case WinApi.Constants.WM_CLOSE:
                    WinApi.PostQuitMessage(0);
                    return;

                case WinApi.Constants.WM_QUIT:
                    quit = true;
                    break;
            }

 	        base.WndProc(ref m);
        }

        private void ProcessKey(ref Message m)
        {
            switch ((int)m.WParam)
            {
                case WinApi.Constants.VK_ESCAPE:
                    Key.Escape = (m.Msg == WinApi.Constants.WM_KEYDOWN) ? true : false;
                    break;
            }
        }

        #endregion

        #region --- IGLWindow Members ---

        #region public void ProcessEvents()

        private System.Windows.Forms.Message msg;
        public void ProcessEvents()
        {
            while (WinApi.PeekMessage(out msg, IntPtr.Zero, 0, 0, 0))
            {
                WinApi.GetMessage(out msg, IntPtr.Zero, 0, 0);
                WndProc(ref msg);
            }
        }

        #endregion

        #region public event CreateEvent Create;

        public event CreateEvent Create;

        private void OnCreate(EventArgs e)
        {
            if (this.Create != null)
            {
                this.Create(this, e);
            }
        }

        #endregion

        #region public bool Quit

        private bool quit;
        public bool Quit
        {
            get { return quit; }
            set
            {
                if (value)
                {
                    WinApi.PostQuitMessage(0);
                    //quit = true;
                }
            }
        }

        #endregion

        #region public IGLContext Context

        public IGLContext Context
        {
            get { return glContext; }
        }

        #endregion

        #region public bool Fullscreen

        bool fullscreen;
        public bool Fullscreen
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region public bool IsIdle

        public bool IsIdle
        {
            get
            {
                return !WinApi.PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
            }
        }

        #endregion

        #endregion

        #region --- IDisposable Members ---

        public void Dispose()
        {
            this.Context.Destroy();
            this.DestroyHandle();
        }

        #endregion

        #region --- IResizable Members ---

        #region public int Width

        public int Width
        {
            get
            {
                return mode.Width;
            }
            set
            {
                throw new NotImplementedException();
                //WinApi.PostMessage(
                //    this.Handle,
                //    WinApi.Constants.WM_WINDOWPOSCHANGING,

                //mode.Width = value;
            }
        }

        #endregion

        #region public int Height

        public int Height
        {
            get
            {
                return mode.Height;
            }
            set
            {
                throw new NotImplementedException();
                //WinApi.PostMessage(
                //    this.Handle,
                //    WinApi.Constants.WM_WINDOWPOSCHANGING,

                //mode.Height = value;
            }
        }

        #endregion

        #region public event ResizeEvent Resize
        public event ResizeEvent Resize;
        private ResizeEventArgs resizeEventArgs = new ResizeEventArgs();
        public void OnResize(ResizeEventArgs e)
        {
            mode.Width = e.Width;
            mode.Height = e.Height;
            if (this.Resize != null)
                this.Resize(this, e);
        }

        #endregion

        #endregion
    }

    #region class WindowHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid

    /*
    class WindowHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        protected override bool ReleaseHandle()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override bool IsInvalid
        {
            get
            {
                return base.IsInvalid;
            }
        }
    }
    */

    #endregion
}
