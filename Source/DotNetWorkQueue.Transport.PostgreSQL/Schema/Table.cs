using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetWorkQueue.Transport.RelationalDatabase;

namespace DotNetWorkQueue.Transport.PostgreSQL.Schema
{
    /// <summary>
    /// Represents a table
    /// </summary>
	public class Table: ITable
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
		public Table(string name) 
        {
			Name = name;
            Columns = new Columns();
            Constraints = new List<Constraint>();
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>
        /// The columns.
        /// </value>
        public Columns Columns { get; set; }
        /// <summary>
        /// Gets or sets the constraints.
        /// </summary>
        /// <value>
        /// The constraints.
        /// </value>
        public List<Constraint> Constraints { get; set; }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }
        /// <summary>
        /// Gets the table information.
        /// </summary>
        /// <value>
        /// The information.
        /// </value>
        public TableInfo Info => new TableInfo(Name);

        /// <summary>
        /// Gets the primary key. Will return null if no primary key is defined.
        /// </summary>
        /// <value>
        /// The primary key.
        /// </value>
        public Constraint PrimaryKey 
        {
            get
            {
                return Constraints.FirstOrDefault(c => c.Type == ConstraintType.PrimaryKey);
            }
        }

        #endregion

        #region Scripting
        /// <summary>
        /// Translates this table into a SQL script
        /// </summary>
        /// <returns></returns>
		public string Script() 
        {
			var text = new StringBuilder();
			text.AppendFormat("CREATE TABLE {0} (\r\n", Name);
			text.Append(Columns.Script());
            if (Constraints.Count > 0)
            {
                text.AppendLine();
            }
            //add anything that is not an index
			foreach (var c in Constraints.Where(c => c.Type != ConstraintType.Index))
			{
			    text.AppendLine("   ," + c.Script());
			}
			text.AppendLine();
            text.AppendLine(");");
            //add indexes
            foreach (var c in Constraints.Where(c => c.Type == ConstraintType.Index))
			{
			    text.AppendLine(c.Script());
                text.AppendLine(";");
            }
			return text.ToString();
        }
        #endregion

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }
    }
}