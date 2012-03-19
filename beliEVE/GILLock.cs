using System;

namespace beliEVE
{

    public class GILLock : IDisposable
    {
        public bool Locked { private set; get; }
        public IntPtr State { private set; get; }

        private void Lock()
        {
            if (Locked)
                return;
            State = Python.GILStateEnsure();
            Locked = true;
        }

        private void Unlock()
        {
            if (Locked)
            {
                Python.GILStateRelease(State);
                Locked = false;
            }
        }

        public GILLock()
        {
            Lock();
        }

        ~GILLock()
        {
            Unlock();
        }

        public void Dispose()
        {
            Unlock();
        }
    }

}