using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace shell
{
    class CEF
    {
    }
    public class GlobalKeyboardHook
    {
        #region Variables and dll import
        #region For key capturing
        [DllImport("user32.dll")]
        static extern int CallNextHookEx(IntPtr hhk, int code, int wParam, ref keyBoardHookStruct lParam);
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LLKeyboardHook callback, IntPtr hInstance, uint theardID);
        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        public delegate int LLKeyboardHook(int Code, int wParam, ref keyBoardHookStruct lParam);

        public struct keyBoardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x0100;
        const int WM_KEYUP = 0x0101;
        const int WM_SYSKEYDOWN = 0x0104;
        const int WM_SYSKEYUP = 0x0105;

        LLKeyboardHook llkh;
        public List<Keys> HookedKeys = new List<Keys>();
        IntPtr Hook = IntPtr.Zero;

        public event KeyEventHandler KeyDown;
        public event KeyEventHandler KeyUp;
        #endregion

        #region For modifier capturing
        /// <summary> 
        /// Gets the state of modifier keys for a given keycode. 
        /// </summary> 
        /// <param name="keyCode">The keyCode</param> 
        /// <returns></returns> 
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);

        //Modifier key vkCode constants 
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12;
        private const int VK_CAPITAL = 0x14;
        #endregion
        #endregion

        #region Constructor
        // This is the Constructor. This is the code that runs every time you create a new GlobalKeyboardHook object
        public GlobalKeyboardHook()
        {
            llkh = new LLKeyboardHook(HookProc);
            // This starts the hook. You can leave this as comment and you have to start it manually 
            // Or delete the comment mark and your hook will start automatically when your program starts (because a new GlobalKeyboardHook object is created)
            // That's why there are duplicates, because you start it twice! I'm sorry, I haven't noticed this...
            // hook(); <-- Choose!
        }
        ~GlobalKeyboardHook()
        { unhook(); }
        #endregion

        #region Functions and implementation
        /// <summary>
        /// Hook (Start listening keybord events)
        /// </summary>
        public void hook()
        {
            IntPtr hInstance = LoadLibrary("User32");
            Hook = SetWindowsHookEx(WH_KEYBOARD_LL, llkh, hInstance, 0);
        }
        /// <summary>
        /// Unhook (Stop listening keybord events)
        /// </summary>
        public void unhook()
        {
            UnhookWindowsHookEx(Hook);
        }
        /// <summary>
        /// Pass key into event
        /// </summary>
        /// <param name="Code">Key code</param>
        /// <param name="wParam">int event type (keydown/keyup)</param>
        /// <param name="lParam">keyBoardHookStruct enum for detecting key</param>
        /// <returns>next hook call</returns>
        public int HookProc(int Code, int wParam, ref keyBoardHookStruct lParam)
        {
            if (Code >= 0)
            {
                Keys key = (Keys)lParam.vkCode;
                if (HookedKeys.Contains(key))
                {
                    //Get modifiers 
                    key = AddModifiers(key);
                    KeyEventArgs kArg = new KeyEventArgs(key);
                    if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && (KeyDown != null))
                    {
                        KeyDown(this, kArg);
                    }
                    else if ((wParam == WM_KEYUP || wParam == WM_SYSKEYUP) && (KeyUp != null))
                    {
                        KeyUp(this, kArg);
                    }
                    if (kArg.Handled)
                        return 1;
                }
            }
            return CallNextHookEx(Hook, Code, wParam, ref lParam);
        }
        /// <summary> 
        /// Checks whether Alt, Shift, Control or CapsLock 
        /// is pressed at the same time as the hooked key. 
        /// Modifies the keyCode to include the pressed keys. 
        /// </summary> 
        private Keys AddModifiers(Keys key)
        {
            //CapsLock 
            if ((GetKeyState(VK_CAPITAL) & 0x0001) != 0) key = key | Keys.CapsLock;
            //Shift 
            if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) key = key | Keys.Shift;
            //Ctrl 
            if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) key = key | Keys.Control;
            //Alt 
            if ((GetKeyState(VK_MENU) & 0x8000) != 0) key = key | Keys.Alt;
            return key;
        }
        #endregion
    }
    public class ApplicationFocus
    {
        /// <summary>Returns true if the current application has focus, false otherwise</summary>
        public static bool ApplicationIsActivated()
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
            {
                return false;       // No window is currently activated
            }

            var procId = Process.GetCurrentProcess().Id;
            int activeProcId;
            GetWindowThreadProcessId(activatedHandle, out activeProcId);

            return activeProcId == procId;
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);
    }
}
