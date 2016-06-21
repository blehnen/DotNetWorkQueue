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
namespace DotNetWorkQueue.Transport.SqlServer.Schema
{
    /// <summary>
    /// Represents an identity property of a column
    /// </summary>
	public class Identity
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Identity"/> class.
        /// </summary>
        /// <param name="seed">The seed.</param>
        /// <param name="increment">The increment.</param>
		public Identity(int seed, int increment) 
        {
            Seed = seed;
            Increment = increment;
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// The increment
        /// </summary>
        /// <value>
        /// The increment.
        /// </value>
        public int Increment { get; set; }
        /// <summary>
        /// The seed
        /// </summary>
        /// <value>
        /// The seed.
        /// </value>
        public int Seed { get; set; }
        #endregion

        #region Scripting
        /// <summary>
        /// Translates this identity into a SQL script
        /// </summary>
        /// <returns></returns>
        public string Script() 
        {
			return $"IDENTITY ({Seed},{Increment})";
		}
        #endregion

        #region Clone
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public Identity Clone()
        {
            return new Identity(Seed, Increment);
        }
        #endregion
    }
}