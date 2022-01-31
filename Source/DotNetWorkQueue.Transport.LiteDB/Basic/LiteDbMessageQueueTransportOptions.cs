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

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Transport options. Generally speaking, this controls the feature set of the transport.
    /// </summary>
    public class LiteDbMessageQueueTransportOptions : IReadonly, ISetReadonly, IBaseTransportOptions
    {
        private bool _enableStatusTable;
        private bool _enableRoute;
        private bool _enableDelayedProcessing;
        private bool _enableMessageExpiration;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbMessageQueueTransportOptions" /> class.
        /// </summary>
        public LiteDbMessageQueueTransportOptions()
        {
            EnableStatusTable = false;
            EnableRoute = false;
            EnableMessageExpiration = false;
            EnableDelayedProcessing = false;
        }
        #endregion

        #region Options
        /// <summary>
        /// Gets or sets a value indicating whether [enable priority].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable priority]; otherwise, <c>false</c>.
        /// </value>
        public bool EnablePriority => false;

        /// <summary>
        /// Gets or sets a value indicating whether [enable status].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable status]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableStatus => true;

        /// <summary>
        /// Gets or sets a value indicating whether [enable heart beat].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable heart beat]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableHeartBeat => true;
        /// <summary>
        /// Gets or sets a value indicating whether [enable delayed processing].
        /// </summary>
        /// <value>
        /// <c>true</c> if [enable delayed processing]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableDelayedProcessing
        {
            get => _enableDelayedProcessing;
            set
            {
                FailIfReadOnly();
                _enableDelayedProcessing = value;
            }
        }


        /// <summary>
        /// Gets or sets a value indicating whether [enable status table].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable status table]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableStatusTable
        {
            get => _enableStatusTable;
            set
            {
                FailIfReadOnly();
                _enableStatusTable = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether routing is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable route]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableRoute
        {
            get => _enableRoute;
            set
            {
                FailIfReadOnly();
                _enableRoute = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [enable message expiration].
        /// </summary>
        /// <value>
        /// <c>true</c> if [enable message expiration]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableMessageExpiration
        {
            get => _enableMessageExpiration;
            set
            {
                FailIfReadOnly();
                _enableMessageExpiration = value;
            }
        }
        #endregion

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
    }
}
