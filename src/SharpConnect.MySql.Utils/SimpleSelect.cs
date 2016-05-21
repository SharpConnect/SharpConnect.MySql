//MIT 2015, brezza92, EngineKit and contributors
using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

namespace SharpConnect.MySql.Utils
{

    class DataRecordTypePlan
    {
        public TypePlanKind planKind;
        public List<DataFieldPlan> fields = new List<DataFieldPlan>();

        public void AssignData(object o, MySqlDataReader reader)
        {
            switch (planKind)
            {
                case TypePlanKind.AllFields:
                    {

                        //TODO: review here 
                        int j = fields.Count;
                        for (int i = 0; i < j; ++i)
                        {
                            //sample only ***
                            //plan: use dynamic method ***
                            fields[i].fieldInfo.SetValue(o, reader.GetString(i));
                        }
                    }
                    break;
                case TypePlanKind.AllProps:
                    {
                        //TODO: review here 
                        int j = fields.Count;
                        object[] invokeArgs = new object[1];
                        for (int i = 0; i < j; ++i)
                        {
                            //sample only ***
                            //plan: use dynamic method ***
                            invokeArgs[0] = reader.GetString(i);
                            fields[i].propSetMethodInfo.Invoke(o, invokeArgs);
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }

        }
    }
    enum TypePlanKind
    {
        MaybeAnonymousType,
        AllProps,
        AllFields
    }
    class DataFieldPlan
    {
        public readonly Type type;
        public readonly string name;
        public readonly FieldInfo fieldInfo;
        public readonly MethodInfo propSetMethodInfo;
        public DataFieldPlan(ParameterInfo pinfo)
        {
            name = pinfo.Name;
            type = pinfo.ParameterType;

        }
        public DataFieldPlan(FieldInfo fieldInfo)
        {
            name = fieldInfo.Name;
            type = fieldInfo.FieldType;
            this.fieldInfo = fieldInfo;

            var fieldNameAttr = fieldInfo.GetCustomAttribute(typeof(FieldNameAttribute)) as FieldNameAttribute;
            if (fieldNameAttr != null)
            {
                name = fieldNameAttr.FieldName;
            }
        }
        public DataFieldPlan(PropertyInfo propInfo)
        {
            name = propInfo.Name;
            type = propInfo.PropertyType;
            propSetMethodInfo = propInfo.SetMethod;

            var fieldNameAttr = propInfo.GetCustomAttribute(typeof(FieldNameAttribute)) as FieldNameAttribute;
            if (fieldNameAttr != null)
            {
                name = fieldNameAttr.FieldName;
            }
        }
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

        public SeqRecReader(MySqlDataReader mysqlDataReader)
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

        public void ResetColumnPos()
        {
            _colNumber = 0;
        }
    }
    public class SimpleSelect : IHasParameters
    {
        MySqlCommand _sqlCommand;
        bool _isPrepared;
        string _whereClause;


        static Dictionary<Type, DataRecordTypePlan> typePlanCaches = new Dictionary<Type, DataRecordTypePlan>();

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

            DataRecordTypePlan foundPlan;
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
        public IEnumerable<T> ExecRecordIter<T>(Func<T> createNewItem)
        {

            DataRecordTypePlan foundPlan;
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
            while (reader.Read())
            {
                T d = createNewItem();
                foundPlan.AssignData(d, reader);
                yield return d;
            }
            reader.Close();
        }
        static DataRecordTypePlan CreateTypePlan(Type t)
        {
            if (t.IsPrimitive)
            {
                return null;
            }
            //-----------------
            //check public ctor
            ConstructorInfo[] allCtors = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

            //type layout check 
            //1. find default ctor 
            int ctorCount = allCtors.Length;
            if (ctorCount == 0)
            {
                return null;
            }


            ConstructorInfo defaultParameterLessCtor = null;
            for (int i = 0; i < ctorCount; ++i)
            {
                ConstructorInfo ctor = allCtors[i];
                ParameterInfo[] ctorParams = ctor.GetParameters();
                if (ctorParams.Length == 0)
                {
                    defaultParameterLessCtor = ctor;
                    break;
                }
                else
                {

                }
            }

            //------------------------------------------------- 
            //note, in this version we have some restriction
            //1. must be class
            //2. must be sealed
            //3. have 1 ctor 

            if (!t.IsSealed && allCtors.Length > 0)
            {
                return null;
            }

            if (t.IsAutoLayout)
            {
                if (defaultParameterLessCtor != null)
                {
                    //have parameter less ctor
                    //then
                    //more restrictions

                    //1. all public fields, no properties
                    //2. all public properties, no public fields

                    //TODO: 
                    //impl:

                    PropertyInfo[] allProps = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                    FieldInfo[] allFields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                    if (allProps.Length > 0 && allFields.Length == 0)
                    {
                        var typePlan = new DataRecordTypePlan();
                        int j = allProps.Length;
                        for (int i = 0; i < j; ++i)
                        {
                            PropertyInfo propInfo = allProps[i];
                            if (propInfo.GetMethod == null || propInfo.SetMethod == null)
                            {
                                return null;
                            }
                            var fieldPlan = new DataFieldPlan(propInfo);
                            typePlan.fields.Add(fieldPlan);
                        }
                        typePlan.planKind = TypePlanKind.AllProps;
                        return typePlan;
                    }
                    else if (allFields.Length > 0)
                    {
                        //not readonly field
                        var typePlan = new DataRecordTypePlan();
                        int j = allFields.Length;
                        for (int i = 0; i < j; ++i)
                        {
                            FieldInfo fieldInfo = allFields[i];

                            var fieldPlan = new DataFieldPlan(fieldInfo);
                            typePlan.fields.Add(fieldPlan);
                        }
                        typePlan.planKind = TypePlanKind.AllFields;
                        return typePlan;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {

                    //guess that type is anonymous type

                    //no default parameter less ctro
                    //only 1 exeption, 
                    //for anonymous type 
                    //-------------------------------------------------
                    //get public property get,set only
                    //single layer only
                    PropertyInfo[] allProps = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                    //all prop has no set method 
                    int j = allProps.Length;
                    for (int i = 0; i < j; ++i)
                    {
                        PropertyInfo propInfo = allProps[i];
                        if (propInfo.SetMethod != null)
                        {
                            //not anonyomus type
                            return null;
                        }
                    }
                    ParameterInfo[] ctorParams = allCtors[0].GetParameters();
                    if (ctorParams.Length != allProps.Length)
                    {
                        return null; //not anonyomus type
                    }
                    //---------------------------------------------------------
                    //guess that this is anonymous type
                    //since type is auto layout
                    //so we use field order according to its ctor parameters 
                    var typePlan = new DataRecordTypePlan();
                    for (int i = 0; i < j; ++i)
                    {
                        ParameterInfo pinfo = ctorParams[i];
                        typePlan.fields.Add(new DataFieldPlan(pinfo));
                    }
                    typePlan.planKind = TypePlanKind.MaybeAnonymousType;
                    return typePlan;
                }

            }
            else if (t.IsLayoutSequential)
            {

                //assign by fieldname *** 
                if (defaultParameterLessCtor == null)
                {
                    //TODO: impl here
                    return null;
                }
                else 
                {
                    //have parameter less ctor
                    //then
                    //more restrictions

                    //1. all public fields, no properties
                    //2. all public properties, no public fields

                    //TODO: 
                    //impl:

                    PropertyInfo[] allProps = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                    FieldInfo[] allFields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                    if (allProps.Length > 0 && allFields.Length == 0)
                    {
                        var typePlan = new DataRecordTypePlan();
                        int j = allProps.Length;
                        for (int i = 0; i < j; ++i)
                        {
                            PropertyInfo propInfo = allProps[i];
                            if (propInfo.GetMethod == null || propInfo.SetMethod == null)
                            {
                                return null;
                            }
                            var fieldPlan = new DataFieldPlan(propInfo);
                            typePlan.fields.Add(fieldPlan);
                        }
                        typePlan.planKind = TypePlanKind.AllProps;
                        return typePlan;
                    }
                    else if (allFields.Length > 0)
                    {
                        //not readonly field
                        var typePlan = new DataRecordTypePlan();
                        int j = allFields.Length;
                        for (int i = 0; i < j; ++i)
                        {
                            FieldInfo fieldInfo = allFields[i];

                            var fieldPlan = new DataFieldPlan(fieldInfo);
                            typePlan.fields.Add(fieldPlan);
                        }
                        typePlan.planKind = TypePlanKind.AllFields;
                        return typePlan;
                    }
                    else
                    {
                        return null;
                    }
                }

            }
            else
            {
                throw new Exception("not supported layout");
            } 


        }



        StringBuilder CreateSqlText(DataRecordTypePlan typePlan)
        {
            CommandParams pars = Pars;
            string[] valueKeys = pars.GetAttachedValueKeys();
            var stBuilder = new StringBuilder();
            stBuilder.Append("select ");

            int j = typePlan.fields.Count;
            for (int i = 0; i < j; ++i)
            {
                DataFieldPlan fieldPlan = typePlan.fields[i];
                stBuilder.Append(fieldPlan.name);
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