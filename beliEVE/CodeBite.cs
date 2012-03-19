using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace beliEVE
{

    /// <summary>
    /// Guarantees synchronous execution of a piece (a bite) of code in the python environment
    /// </summary>
    public class CodeBite
    {
        private readonly ManualResetEventSlim _executed = new ManualResetEventSlim(false);
        private readonly Python.PendingCallFunc _func;
        private readonly RunFunction _runFunc;
        private Exception _exception;
        protected string Code;

        public CodeBite(RunFunction func)
        {
            _func = RunFuncInternal;
            _runFunc = func;
            _exception = null;
        }

        public CodeBite(string code)
        {
            Code = code;
            _func = RunCodeInternal;
            _exception = null;
        }

        public IntPtr Result { get; private set; }

        public IntPtr Run()
        {
            Python.AddPendingCall(Marshal.GetFunctionPointerForDelegate(_func), IntPtr.Zero);
            _executed.Wait();
            if (_exception != null)
                throw _exception;
            return Result;
        }

        private int RunFuncInternal(IntPtr unused)
        {
            try
            {
                _runFunc();
                return 0;
            }
            catch (Exception ex)
            {
                _exception = ex;
                return 0;
            }
            finally
            {
                _executed.Set();
            }
        }

        private int RunCodeInternal(IntPtr unused)
        {
            try
            {
                Result = Python.RunPure(Code);

                if (Python.ErrorSet)
                {
                    Python.PrintError();
                    Python.ClearError();
                }
                return 0;
            }
            catch (Exception ex)
            {
                _exception = ex;
                return 0;
            }
            finally
            {
                _executed.Set();
            }
        }
    }

}