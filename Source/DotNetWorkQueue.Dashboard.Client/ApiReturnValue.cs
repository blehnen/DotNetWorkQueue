// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Net;

namespace DotNetWorkQueue.Dashboard.Client
{
    /// <summary>
    /// Wraps the result of a Dashboard API call with status information instead of throwing on non-2xx responses.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    public class ApiReturnValue<T>
    {
        /// <summary>
        /// Gets whether the API call was successful (2xx status code).
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the HTTP status code returned by the API.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Gets the error message if the call was not successful, or null on success.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the deserialized response value. Default if the call was not successful.
        /// </summary>
        public T Value { get; }

        private ApiReturnValue(bool success, HttpStatusCode statusCode, string errorMessage, T value)
        {
            Success = success;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
            Value = value;
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="value">The deserialized response value.</param>
        /// <returns>A successful <see cref="ApiReturnValue{T}"/>.</returns>
        public static ApiReturnValue<T> Ok(HttpStatusCode statusCode, T value)
        {
            return new ApiReturnValue<T>(true, statusCode, null, value);
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="errorMessage">The error message or response body.</param>
        /// <returns>A failed <see cref="ApiReturnValue{T}"/>.</returns>
        public static ApiReturnValue<T> Fail(HttpStatusCode statusCode, string errorMessage)
        {
            return new ApiReturnValue<T>(false, statusCode, errorMessage, default);
        }
    }
}
