﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Animation;

namespace FDK
{
    public class アニメーション管理 : IDisposable
    {
        public Manager Manager { get; private set; } = null;
        public Timer Timer { get; private set; } = null;
        public TransitionLibrary TrasitionLibrary { get; private set; } = null;

        public アニメーション管理()
        {
            this.Manager = new Manager();
            this.Timer = new Timer();
            this.TrasitionLibrary = new TransitionLibrary();
        }
        public void 進行する()
        {
            this.Manager.Update( this.Timer.Time );
        }
        public void Dispose()
        {
            this.TrasitionLibrary?.Dispose();
            this.TrasitionLibrary = null;

            this.Timer?.Dispose();
            this.Timer = null;

            this.Manager?.Dispose();
            this.Manager = null;
        }
    }
}