//MIT 2015, brezza27, EngineKit and contributors
using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

namespace SharpConnect.MySql.Utils
{

    class TypePlan
    {
        public List<FieldPlan> fields = new List<FieldPlan>();
    }
    class FieldPlan
    {
        public PropertyInfo propInfo;
        public string fieldName;
    }
    public class FieldNameAttribute : Attribute
    {
        public FieldNameAttribute(string sqlFieldName)
        {
            FieldName = sqlFieldName;
        }
        public string FieldName { get; set; }
    }


    /// <summary>
    /// sequence record reader
    /// </summary>
    public class SeqRecReader
    {
        int _colNumber = 0;
        MySqlDataReader _mysqlDataReader;

        internal SeqRecReader(MySqlDataReader mysqlDataReader)
        {
            _mysqlDataReader = mysqlDataReader;
        }

        /// <summary>
        /// read string
        /// </summary>
        /// <returns></returns>
        public string str()
        {
            return _mysqlDataReader.GetString(_colNumber++);
        }
        public int int32()
        {
            return _mysqlDataReader.GetInt32(_colNumber++);
        }
        public uint uint32()
        {
            return _mysqlDataReader.GetUInt32(_colNumber++);
        }
        public long int64()
        {
            return _mysqlDataReader.GetLong(_colNumber++);
        }
        public ulong uint64()
        {
            return _mysqlDataReader.GetULong(_colNumber++);
        }
        public DateTime datetime()
        {
            return _mysqlDataReader.GetDateTime(_colNumber++);
        }

        /// <summary>
        /// float, 32 bits,
        /// </summary>
        /// <returns></returns>
        public float f32()
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// double, float64 bits
        /// </summary>
        /// <returns></returns>
        public double f64()
        {
            throw new NotSupportedException();
        }

        public byte[] bytes()
        {
            return _mysqlDataReader.GetBuffer(_colNumber++);
        }
        public decimal Decimal()
        {
            return _mysqlDataReader.GetDecimal(_colNumber++);
        }

        internal void ResetColumnPos()
        {
            _colNumber = 0;
        }
    }
    public class SimpleSelect : IHasParameters
    {
        MySqlCommand _sqlCommand;
        bool _isPrepared;
        string _whereClause;


        static Dictionary<Type, TypePlan> typePlanCaches = new Dictionary<Type, TypePlan>();

        public SimpleSelect(string targetTableName)
        {
            TargetTableName = targetTableName;
            Pars = new CommandParams();
        }
        public CommandParams Pars
        {
            get;
            private set;
        }

        public string TargetTableName { get; private set; }
        public MySqlConnection Connection { get; set; }
        public void Where(string sqlWhere)
        {
            _whereClause = sqlWhere;
        }

        public string Limit { get; set; }

        public IEnumerable<T> ExecRecordIter<T>(Func<SeqRecReader, T> createNewItem)
        {

            TypePlan foundPlan;
            Type itemType = typeof(T);
            if (!typePlanCaches.TryGetValue(itemType, out foundPlan))
            {
                foundPlan = CreateTypePlan(itemType);
                typePlanCaches.Add(itemType, foundPlan);
            }
            //-----------------------------
            //create query
            StringBuilder sql = CreateSqlText(foundPlan);
            var cmd = new MySqlCommand(sql.ToString(), Pars, Connection);
            MySqlDataReader reader = cmd.ExecuteReader();
            var seqReader = new SeqRecReader(reader);

            while (reader.Read())
            {
                yield return createNewItem(seqReader);
                seqReader.ResetColumnPos();
            }
            reader.Close();
        }
        static TypePlan CreateTypePlan(Type t)
        {
            if (t.IsPrimitive)
            {
                return null;
            }
            //-----------------
            //check public ctor
            ConstructorInfo[] allCtors = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

            //1. find default ctor 
            int ctorCount = allCtors.Length;
            if (ctorCount == 0)
            {
                return null;
            }

            var plan = new TypePlan();
            ConstructorInfo defaultParameterLessCtor = null;
            for (int i = 0; i < ctorCount; ++i)
            {
                ConstructorInfo ctor = allCtors[i];
                ParameterInfo[] ctorParams = ctor.GetParameters();
                if (ctorParams.Length == 0)
                {
                    defaultParameterLessCtor = ctor;
                }
                else
                {

                }
            }


            //-------------------------------------------------
            //get public property get,set only
            //single layer only
            PropertyInfo[] allProps = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            int j = allProps.Length;
            for (int i = 0; i < j; ++i)
            {

                PropertyInfo propInfo = allProps[i];
                FieldNameAttribute fieldNameAttr = propInfo.GetCustomAttribute(typeof(FieldNameAttribute)) as FieldNameAttribute;
                var fieldPlan = new FieldPlan();
                fieldPlan.propInfo = propInfo;

                if (fieldNameAttr != null)
                {
                    fieldPlan.fieldName = fieldNameAttr.FieldName;
                }
                else
                {
                    fieldPlan.fieldName = propInfo.Name;
                }
                plan.fields.Add(fieldPlan);
            }


            return plan;
        }



        StringBuilder CreateSqlText(TypePlan typePlan)
        {
            CommandParams pars = Pars;
            string[] valueKeys = pars.GetAttachedValueKeys();
            var stBuilder = new StringBuilder();
            stBuilder.Append("select ");

            int j = typePlan.fields.Count;
            for (int i = 0; i < j; ++i)
            {
                FieldPlan fieldPlan = typePlan.fields[i];
                stBuilder.Append(fieldPlan.fieldName);
                if (i < j - 1)
                {
                    stBuilder.Append(',');
                }
            }

            stBuilder.Append(" from ");
            stBuilder.Append(TargetTableName);

            if (_whereClause != null)
            {
                stBuilder.Append(" where ");
                stBuilder.Append(_whereClause);
            }

            if (Limit != null)
            {
                stBuilder.Append(" limit " + Limit);
            }
            return stBuilder;
        }
    }
}