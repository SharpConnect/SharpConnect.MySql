//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;

namespace SharpConnect.MySql.Internal
{
    static class MySqlTypeConversionInfo
    {
        //built in type conversion 
        static Dictionary<Type, ProperDataType> dataTypeMaps = new Dictionary<Type, ProperDataType>();
        static Dictionary<MySqlDataType, MySqlTypeConversionPlan> implicitConvPlan = new Dictionary<MySqlDataType, MySqlTypeConversionPlan>();
        static MySqlTypeConversionInfo()
        {
            //-----------------------------------------------------------
            dataTypeMaps.Add(typeof(bool), ProperDataType.Bool);
            dataTypeMaps.Add(typeof(byte), ProperDataType.Byte);
            dataTypeMaps.Add(typeof(sbyte), ProperDataType.Sbyte);
            dataTypeMaps.Add(typeof(char), ProperDataType.Char);
            dataTypeMaps.Add(typeof(Int16), ProperDataType.Int16);
            dataTypeMaps.Add(typeof(UInt16), ProperDataType.UInt16);
            dataTypeMaps.Add(typeof(int), ProperDataType.Int32);
            dataTypeMaps.Add(typeof(uint), ProperDataType.UInt32);
            dataTypeMaps.Add(typeof(long), ProperDataType.Int64);
            dataTypeMaps.Add(typeof(ulong), ProperDataType.UInt64);
            dataTypeMaps.Add(typeof(float), ProperDataType.Float32);
            dataTypeMaps.Add(typeof(double), ProperDataType.Double64);
            dataTypeMaps.Add(typeof(DateTime), ProperDataType.DateTime);
            dataTypeMaps.Add(typeof(string), ProperDataType.String);
            dataTypeMaps.Add(typeof(byte[]), ProperDataType.Buffer);
            //-----------------------------------------------------------  
            {
                //mysql src is blob
                var plan = new MySqlTypeConversionPlan(MySqlDataType.BLOB);
                //target
                plan.AddConvTarget(typeof(byte[]), MySqlDataConversionTechnique.Direct);
                plan.AddConvTarget(typeof(string), MySqlDataConversionTechnique.BlobToString);
                Register(plan);
            }
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.LONG_BLOB);
                //target
                plan.AddConvTarget(typeof(byte[]), MySqlDataConversionTechnique.Direct);
                plan.AddConvTarget(typeof(string), MySqlDataConversionTechnique.BlobToString);
                Register(plan);
            }
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.MEDIUM_BLOB);
                //target
                plan.AddConvTarget(typeof(byte[]), MySqlDataConversionTechnique.Direct);
                plan.AddConvTarget(typeof(string), MySqlDataConversionTechnique.BlobToString);
                Register(plan);
            }

            //----------------------------------------------------------- 
            {
                //mysql src is string
                var plan = new MySqlTypeConversionPlan(MySqlDataType.STRING);
                //target 
                plan.AddConvTarget(typeof(string), MySqlDataConversionTechnique.StringToString);
                Register(plan);
            }
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.VARCHAR);
                //target 
                plan.AddConvTarget(typeof(string), MySqlDataConversionTechnique.StringToString);
                Register(plan);
            }
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.VAR_STRING);
                //target 
                plan.AddConvTarget(typeof(string), MySqlDataConversionTechnique.StringToString);
                Register(plan);
            }
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.DECIMAL);
                //target 
                plan.AddConvTarget(typeof(float), MySqlDataConversionTechnique.DecimalToFloat);
                plan.AddConvTarget(typeof(double), MySqlDataConversionTechnique.DecimalToDouble);
                plan.AddConvTarget(typeof(decimal), MySqlDataConversionTechnique.DecimalToDecimal);
                Register(plan);
            }
            //----------------------------------------------------------- 
            {
                //TODO: review here
                //1. if we use prepared statement, we have information about signed/unsiged field
                //see ... a flag byte which has the highest bit set if the type is unsigned [80]  ...
                //https://dev.mysql.com/doc/internals/en/com-stmt-execute.html#packet-COM_STMT_EXECUTE
                //
                //2. if we not use prepared statement then ..
                //WARNING: 
                //my sql not store sign or unsign data in field packet ?
                //but we can get it from sql that show table information
                //so only at this point, we have a tiny (int) -> it can be interpreted as byte or sbyte?              




                var plan = new MySqlTypeConversionPlan(MySqlDataType.TINY); //1 byte int     
                plan.AddConvTargets(
                  new[] {  typeof(byte),typeof(char),
                           typeof(short),typeof(ushort),
                           typeof(int),typeof(uint),
                           typeof(long),typeof(double),
                           typeof(decimal)
                        },
                        MySqlDataConversionTechnique.Direct);
                //TODO: review sbyte?
                //conv bool  
                RegisterWithDefaultStringConv(plan);
            }
            //----------------------------------------------------------- 
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.SHORT); //2 byte int 

                plan.AddConvTargets(
                 new[] {   typeof(char),
                           typeof(short),typeof(ushort),
                           typeof(int),typeof(uint),
                           typeof(long),typeof(ulong),
                           typeof(float),typeof(double),typeof(decimal)
                       },
                       MySqlDataConversionTechnique.Direct);
                RegisterWithDefaultStringConv(plan);
            }

            //----------------------------------------------------------- 
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.INT24); //3 byte int 

                plan.AddConvTargets(
                 new[] {   typeof(int),typeof(uint),
                           typeof(long),typeof(ulong),
                           typeof(float),typeof(double),typeof(decimal)
                       },
                       MySqlDataConversionTechnique.Direct);
                RegisterWithDefaultStringConv(plan);
            }
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.LONG); //4 byte int
                plan.AddConvTargets(
                new[] {   typeof(int),typeof(uint),
                          typeof(long),typeof(ulong),
                          typeof(float),typeof(double),typeof(decimal)
                      },
                      MySqlDataConversionTechnique.Direct);
                RegisterWithDefaultStringConv(plan);
            }
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.LONGLONG); //8 byte int 
                //target
                plan.AddConvTargets(
                 new[] {
                          typeof(long),typeof(ulong),
                          typeof(double),typeof(decimal)
                       },
                       MySqlDataConversionTechnique.Direct);
                RegisterWithDefaultStringConv(plan);
            }
            //-----------------------------------------------------------
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.FLOAT);  //4  byte
                plan.AddConvTargets(
                 new[] {
                          typeof(float),typeof(double),
                          typeof(decimal)
                       },
                       MySqlDataConversionTechnique.Direct);
                RegisterWithDefaultStringConv(plan);
            }
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.DOUBLE);  //4  byte
                plan.AddConvTargets(
                 new[] {
                          typeof(double),
                          typeof(decimal)
                       },
                       MySqlDataConversionTechnique.Direct);
                RegisterWithDefaultStringConv(plan);
            }
            //-----------------------------------------------------------
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.NEWDECIMAL);
                plan.AddConvTargets(
                  new[] {  typeof(decimal)
                        },
                        MySqlDataConversionTechnique.Direct);
                RegisterWithDefaultStringConv(plan);
            }
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.NEWDATE);
                RegisterWithDefaultStringConv(plan);
            }
            //----------------------------------------------------------- 
            {

                var plan = new MySqlTypeConversionPlan(MySqlDataType.DATE);
                plan.AddConvTarget(typeof(DateTime), MySqlDataConversionTechnique.GenDateTime);
                RegisterWithDefaultStringConv(plan);
            }
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.TIME);
                plan.AddConvTarget(typeof(DateTime), MySqlDataConversionTechnique.GenDateTime);
                RegisterWithDefaultStringConv(plan);
            }
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.DATETIME);
                plan.AddConvTarget(typeof(DateTime), MySqlDataConversionTechnique.GenDateTime);
                RegisterWithDefaultStringConv(plan);
            }
            {
                var plan = new MySqlTypeConversionPlan(MySqlDataType.TIMESTAMP);
                plan.AddConvTarget(typeof(DateTime), MySqlDataConversionTechnique.GenDateTime);
                RegisterWithDefaultStringConv(plan);
            }
            //----------------------------------------------------------- 
        }
        public static ProperDataType GetProperDataType(object o)
        {
            ProperDataType foundProperType;
            Type o_type;
            if (!dataTypeMaps.TryGetValue(o_type = o.GetType(), out foundProperType))
            {
                return ProperDataType.Unknown;
            }
            return foundProperType;
        }
        static void RegisterWithDefaultStringConv(MySqlTypeConversionPlan plan)
        {
            //for all plan
            plan.AddConvTarget(typeof(string), MySqlDataConversionTechnique.GenString);
            implicitConvPlan.Add(plan.SourceMySqlType, plan);
        }
        static void Register(MySqlTypeConversionPlan plan)
        {
            implicitConvPlan.Add(plan.SourceMySqlType, plan);
        }
        public static bool TryGetImplicitConversion(MySqlDataType mysqlDataType, Type targetType, out MySqlDataConversionTechnique foundTechnique)
        {
            MySqlTypeConversionPlan convPlan;
            if (implicitConvPlan.TryGetValue(mysqlDataType, out convPlan))
            {
                //found conv for some mysql data type
                if (convPlan.TryGetConvTechnique(targetType, out foundTechnique))
                {
                    return true;
                }
            }
            //not found
            foundTechnique = MySqlDataConversionTechnique.Unknown;
            return false;
        }
    }

    enum MySqlDataConversionTechnique : byte
    {
        Unknown,
        Direct,
        StringToString,
        BlobToString,
        GenDateTime,
        GenString,
        Custom,
        DecimalToFloat,
        DecimalToDouble,
        DecimalToDecimal,
    }

    class MySqlTypeConversionPlan
    {
        Dictionary<Type, MySqlDataConversionTechnique> convTechniques = new Dictionary<Type, MySqlDataConversionTechnique>();

        public MySqlTypeConversionPlan(MySqlDataType sourceMySqlType)
        {
            this.SourceMySqlType = sourceMySqlType;
        }
        public void AddConvTarget(Type targetType, MySqlDataConversionTechnique convTech)
        {
            this.convTechniques.Add(targetType, convTech);
        }
        public void AddConvTargets(Type[] targetTypes, MySqlDataConversionTechnique convTech)
        {
            for (int i = targetTypes.Length - 1; i >= 0; --i)
            {
                convTechniques.Add(targetTypes[i], convTech);
            }
        }
        public MySqlDataType SourceMySqlType
        {
            get;
            private set;
        }

        public bool TryGetConvTechnique(Type targetType, out MySqlDataConversionTechnique found)
        {
            return convTechniques.TryGetValue(targetType, out found);
        }
    }
}
