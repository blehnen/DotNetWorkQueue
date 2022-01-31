// ---------------------------------------------------------------------
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
using System.Text;

namespace DotNetWorkQueue.Transport.Memory
{
    /// <summary>
    /// Transport options. Generally speaking, this controls the feature set of the transport.
    /// </summary>
    public class TransportOptions : IReadonly, ISetReadonly, IBaseTransportOptions
    {
        #region Validation
        /// <summary>
        /// Validates the configuration settings
        /// </summary>
        /// <returns></returns>
        public Validation ValidConfiguration()
        {
            var v = new Validation();
            var sbErrors = new StringBuilder();
            v.Valid = true;

            v.ErrorMessage = sbErrors.ToString();

            if (!string.IsNullOrWhiteSpace(v.ErrorMessage))
                v.Valid = false;

            return v;
        }
        #endregion

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Throws an exception if the readonly flag is true.
        /// </summary>
        /// <exception cref="System.Data.ReadOnlyException"></exception>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <summary>
        /// Marks this instance as immutable
        /// </summary>
        public void SetReadOnly()
        {
            IsReadOnly = true;
        }

        /// <summary>
        /// Configuration validation status
        /// </summary>
        public class Validation
        {
            /// <summary>
            /// Gets or sets a value indicating whether the configuration is valid.
            /// </summary>
            /// <value>
            ///   <c>true</c> if valid; otherwise, <c>false</c>.
            /// </value>
            public bool Valid { get; set; }
            /// <summary>
            /// Gets or sets the error message.
            /// </summary>
            /// <value>
            /// The error message.
            /// </value>
            public string ErrorMessage { get; set; }
        }

        /// <inheritdoc/>
        public bool EnablePriority => false;
        /// <inheritdoc/>
        public bool EnableStatus => false;
        /// <inheritdoc/>
        public bool EnableHeartBeat => false;
        /// <inheritdoc/>
        public bool EnableDelayedProcessing => false;
        /// <inheritdoc/>
        public bool EnableStatusTable => false;
        /// <inheritdoc/>
        public bool EnableRoute => false;
        /// <inheritdoc/>
        public bool EnableMessageExpiration => false;
    }
}
