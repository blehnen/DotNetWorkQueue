﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Linq.Expressions;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Serializes linq expression trees to byte[] and back
    /// </summary>
    public interface IExpressionSerializer
    {
        /// <summary>
        /// Converts the action method to bytes.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns></returns>
        byte[] ConvertMethodToBytes(Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> method);

        /// <summary>
        /// Converts the bytes to an action method.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> ConvertBytesToMethod(byte[] bytes);
    }
}
