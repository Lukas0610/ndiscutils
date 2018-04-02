/*
 * nDiscUtils - Advanced utilities for disc management
 * Copyright (C) 2018  Lukas Berger
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace nDiscUtils
{

    public sealed class ILMethod
    {

        private static int kIlMethodCount = 0;
        
        private string mName;
        private Type mReturnType;

        private List<OpCode> mOpCodes;
        private DynamicMethod mDynamicMethod;

        private Delegate mDelegate;

        public ILMethod(Type returnType)
        {
            mName = $"ILMethod_{kIlMethodCount}";
            mReturnType = returnType;

            mOpCodes = new List<OpCode>();
            mDynamicMethod = null;

            mDelegate = null;

            kIlMethodCount++;
        }

        public void Write(params OpCode[] opCodes)
            => mOpCodes.AddRange(opCodes);

        public void Generate<T1>()
            => GenerateInternal(typeof(Action<T1>));

        public void Generate<T1, T2>()
            => GenerateInternal(typeof(Action<T1, T2>));

        public void Generate<T1, T2, T3>()
            => GenerateInternal(typeof(Action<T1, T2, T3>));

        public void Generate<T1, T2, T3, T4>()
            => GenerateInternal(typeof(Action<T1, T2, T3, T4>));

        public void Generate<T1, T2, T3, T4, T5>()
            => GenerateInternal(typeof(Action<T1, T2, T3, T4, T5>));

        public void Generate<T1, T2, T3, T4, T5, T6>()
            => GenerateInternal(typeof(Action<T1, T2, T3, T4, T5, T6>));

        public void Generate<T1, T2, T3, T4, T5, T6, T7>()
            => GenerateInternal(typeof(Action<T1, T2, T3, T4, T5, T6, T7>));

        private void GenerateInternal(Type delegateType)
        {
            mDynamicMethod = new DynamicMethod(
                mName,
                mReturnType,
                delegateType.GetGenericArguments(),
                typeof(ILMethod),
                true);

            var ilGenerator = mDynamicMethod.GetILGenerator();
            foreach (var opCode in mOpCodes)
                ilGenerator.Emit(opCode);

            mDelegate = mDynamicMethod.CreateDelegate(delegateType);
        }

        public object Run(params object[] args)
        {
            if (mReturnType == typeof(void))
                mDelegate.DynamicInvoke(args);
            else
                Convert.ChangeType(mDelegate.DynamicInvoke(args), mReturnType);
            return null;
        }

    }

}
