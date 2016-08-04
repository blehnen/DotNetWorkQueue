// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Reflection;
using System.Text;
using Aq.ExpressionJsonSerializer;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;
using Newtonsoft.Json;

namespace DotNetWorkQueue.Serialization
{
    /// <summary>
    /// Serializes a LINQ expression tree to JSON
    /// </summary>
    /// <remarks>The implementation library is not thread safe; thus, this library blocks on multiple calls...</remarks>
    /// <seealso cref="DotNetWorkQueue.IExpressionSerializer" />
    public class JsonExpressionSerializer : IExpressionSerializer
    {
        private readonly JsonSerializerSettings _serializerSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonExpressionSerializer"/> class.
        /// </summary>
        public JsonExpressionSerializer()
        {
            _serializerSettings = new JsonSerializerSettings();
            _serializerSettings.Converters.Add(
                new ExpressionJsonConverter(Assembly.GetAssembly(typeof(JsonExpressionSerializer))));
        }

        /// <summary>
        /// Converts the bytes to an action method.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        public Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> ConvertBytesToMethod(
            byte[] bytes)
        {
            Guard.NotNull(() => bytes, bytes);
            return
                JsonConvert
                    .DeserializeObject<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(
                        Encoding.UTF8.GetString(bytes), _serializerSettings);
        }

        /// <summary>
        /// Converts the action method to bytes.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns></returns>
        public byte[] ConvertMethodToBytes(
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> method)
        {
            Guard.NotNull(() => method, method);
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(method, _serializerSettings));
        }

        /// <summary>
        /// Converts the function to bytes.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns></returns>
        public byte[] ConvertFunctionToBytes(
            Expression<Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object>> method)
        {
            Guard.NotNull(() => method, method);
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(method, _serializerSettings));
        }

        /// <summary>
        /// Converts the bytes back to the function.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        public Expression<Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object>> ConvertBytesToFunction
            (byte[] bytes)
        {
            Guard.NotNull(() => bytes, bytes);
            return
                JsonConvert
                    .DeserializeObject
                    <Expression<Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object>>>(
                        Encoding.UTF8.GetString(bytes), _serializerSettings);
        }
    }
}
