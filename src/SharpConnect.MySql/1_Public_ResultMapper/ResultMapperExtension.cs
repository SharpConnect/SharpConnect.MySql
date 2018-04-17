//MIT, 2015-2018, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using System.Reflection;

using SharpConnect.MySql.Internal;
namespace SharpConnect.MySql.Mapper
{

    public struct MySqlFieldMap
    {
        string fieldname;
        int fieldIndex;
        MySqlDataConversionTechnique convTechnique;
        internal FieldInfo resolvedFieldInfo;

        internal MySqlFieldMap(int originalFieldIndex, string fieldname, MySqlDataConversionTechnique convTechnique)
        {
            this.fieldIndex = originalFieldIndex;
            this.convTechnique = convTechnique;
            this.fieldname = fieldname;
            this.resolvedFieldInfo = null;
        }
        public int OriginalFieldIndex
        {
            get { return fieldIndex; }
        }
        public string FieldName
        {
            get { return fieldname; }
        }
        internal MySqlDataConversionTechnique ConvTechnique
        {
            get
            {
                return convTechnique;
            }
        }
    }

    public abstract class RecordMapBase
    {

        protected MethodInfo metInfo;
        protected List<MySqlFieldMap> mapFields = new List<MySqlFieldMap>();
        protected MySqlDataReader reader;
        protected bool checkTableDefinition;
        protected IStringConverter stringConverter;
        public RecordMapBase(MethodInfo metInfo)
        {
            this.metInfo = metInfo;
        }
        public MySqlDataReader DataReader
        {
            get { return this.reader; }
            set
            {
                this.reader = value;
                checkTableDefinition = false;
            }
        }
        public IStringConverter StringConverter
        {
            get { return stringConverter; }
            set
            {
                stringConverter = value;
            }
        }

        public List<MySqlFieldMap> GetMapFields()
        {
            if (!checkTableDefinition)
            {
                EvaluateTableDefinition(metInfo);
                checkTableDefinition = true;
            }
            return this.mapFields;
        }
        static FieldInfo[] GetPublicInstanceFields(Type t)
        {

#if NET20
            return t.GetFields(BindingFlags.Public | BindingFlags.Instance);
#else
            //var typeInfo = t.GetTypeInfo();
            //return typeInfo.GetFields(BindingFlags.Public | BindingFlags.Instance);            
             
            List<FieldInfo> fields= new List<FieldInfo>();
            foreach(var field in t.GetRuntimeFields()){
                if(field.IsPublic && !field.IsStatic){
                    fields.Add(field);
                }
            }
            return fields.ToArray();
#endif
        }
        protected void EvaluateTargetStructure(Type t)
        {
            //evaluate target objet definition
            //check current table defintioin first ***
            MySqlSubTable subTable = reader.CurrentSubTable;
            //get all public instance fields from current type 
            FieldInfo[] allFields = GetPublicInstanceFields(t);


            //we iterate all request fields
            int j = allFields.Length;
            mapFields.Clear();
            for (int i = 0; i < j; ++i)
            {
                FieldInfo field = allFields[i];
                MySqlFieldDefinition fieldDef = subTable.GetFieldDefinition(field.Name);
                //----------------------------------
                //check field type conversion 
                //1. some basic can do direct conversion
                //2. use can provide custom protocol for field conversion
                //3. some need user decision
                //----------------------------------
                //in this version we support only primitive type  ***
                MySqlDataConversionTechnique foundConv;
                if (!MySqlTypeConversionInfo.TryGetImplicitConversion((MySqlDataType)fieldDef.FieldType,
                    field.FieldType, out foundConv))
                {
                    //not found
                    //TODO: 
                    //so make notification by let use make a dicision
                    throw new NotSupportedException();
                }
                //-----------------
                MySqlFieldMap fieldMap =
                    fieldDef.IsEmpty ?
                     new MySqlFieldMap(-1, fieldDef.Name, foundConv) :
                     new MySqlFieldMap(fieldDef.FieldIndex, fieldDef.Name, foundConv);
                fieldMap.resolvedFieldInfo = field;
                mapFields.Add(fieldMap);
            }
        }

        protected void EvaluateTableDefinition(MethodInfo met)
        {
            //check current table defintioin first ***
            MySqlSubTable subTable = reader.CurrentSubTable;
            var metPars = met.GetParameters();
            //target method that we need 
            int j = metPars.Length;//**
            mapFields.Clear();
            for (int i = 1; i < j; ++i)
            {
                //get parameter fieldname
                //and type and check proper type conversion
                ParameterInfo metPar = metPars[i];
                MySqlFieldDefinition fieldDef = subTable.GetFieldDefinition(metPar.Name);
                //----------------------------------
                //check field type conversion 
                //1. some basic can do direct conversion
                //2. use can provide custom protocol for field conversion
                //3. some need user decision
                //----------------------------------
                //in this version we support only primitive type  ***
                MySqlDataConversionTechnique foundConv;
                if (!MySqlTypeConversionInfo.TryGetImplicitConversion((MySqlDataType)fieldDef.FieldType, metPar.ParameterType, out foundConv))
                {
                    //not found
                    //TODO: 
                    //so make notification by let use make a dicision
                    throw new NotSupportedException();
                }
                //-----------------
                MySqlFieldMap fieldMap =
                    fieldDef.IsEmpty ?
                     new MySqlFieldMap(-1, fieldDef.Name, foundConv) :
                     new MySqlFieldMap(fieldDef.FieldIndex, fieldDef.Name, foundConv);
                mapFields.Add(fieldMap);
            }
        }

        public object RawGetValueOrDefaultFromMapIndex(int mapIndex)
        {
            //this check plan
            //get data from reader at original field 
            //lets do proper conv technique
            MySqlFieldMap mapField = mapFields[mapIndex];
            object value = reader.GetValue(mapField.OriginalFieldIndex);
#if DEBUG
            Type srcType = value.GetType();

#endif

            switch (mapField.ConvTechnique)
            {
                case MySqlDataConversionTechnique.Direct:
                    return value;
                case MySqlDataConversionTechnique.GenString:
                    //gen to string
                    return value.ToString();
                case MySqlDataConversionTechnique.GenDateTime:
                    return DateTime.Parse(value.ToString());
                case MySqlDataConversionTechnique.BlobToString:
                    //
                    return value.ToString();
                case MySqlDataConversionTechnique.StringToString:
                    //
                    if (stringConverter != null)
                    {
                        //use string converter to convert again
                        return stringConverter.ReadConv((string)value);
                    }
                    else
                    {
                        return value.ToString();
                    }
                case MySqlDataConversionTechnique.DecimalToDecimal:
                    {
                        return (decimal)value;
                    }
                case MySqlDataConversionTechnique.DecimalToDouble:
                    {
                        return Convert.ToDouble((decimal)value);
                    }
                case MySqlDataConversionTechnique.DecimalToFloat:
                    {
                        return (float)Convert.ToDouble(value);
                    }
                default:
                    throw new NotSupportedException();
            }
        }
    }


    public class RecordMap<R> : RecordMapBase<R>
    {
        public RecordMap() { }
        protected override void OnMap(R r)
        {
            //do nothing
        }
    }
    public abstract class RecordMapBase<R> : RecordMapBase
    {
        protected RecordMapBase()
            : base(null)
        {
        }
        public RecordMapBase(Delegate del)
            : base(del.GetMethodInfo())
        {
        }
        protected U GetValueOrDefaultFromActualIndex<U>(int actualIndex)
        {
            if (actualIndex < 0)
            {
                return default(U);
            }
            else
            {
                //get actual value from reader

                return (U)reader.GetValue(actualIndex);
            }
        }
        protected U GetValueOrDefaultFromMapIndex<U>(int mapIndex)
        {
            if (mapIndex < 0)
            {
                return default(U);
            }
            else
            {
                return (U)RawGetValueOrDefaultFromMapIndex(mapIndex);

            }
        }
        public R Map(R r)
        {
            //--------------------------------------------------------------
            //TODO: we can optimize how to map data with dynamic method ***
            //--------------------------------------------------------------

            //map data from currrent record in data reader
            if (!checkTableDefinition)
            {
                EvaluateTableDefinition(metInfo);
                checkTableDefinition = true;
            }
            OnMap(r);
            return r;
        }

        public R AutoFill(R r)
        {
            //--------------------------------------------------------------
            //TODO: we can optimize how to map data with dynamic method ***
            //-------------------------------------------------------------- 
            //auto fill value
            if (!checkTableDefinition)
            {
                //get actual type of R
                EvaluateTargetStructure(r.GetType());
                checkTableDefinition = true;
            }
            //after evaluate then we do auto map ***
            int j = mapFields.Count;
            for (int i = 0; i < j; ++i)
            {
                MySqlFieldMap mapField = mapFields[i];
                object value = RawGetValueOrDefaultFromMapIndex(i);
                mapField.resolvedFieldInfo.SetValue(r, value);
            }
            return r;
        }
        //--------------------------------------------------------------
        //TODO: we can optimize how to map data with dynamic method ***
        //--------------------------------------------------------------

        protected abstract void OnMap(R r);

    }

    public class RecordMap<R, T> : RecordMapBase<R>
    {
        MapAction<R, T> recordMapDel;

        public RecordMap(MapAction<R, T> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            //--------------------------------------------------------------
            //TODO: we can optimize how to map data with dynamic method ***
            //--------------------------------------------------------------

            //after evaluate table definition
            //we can use field map to map value of record and then invoke
            recordMapDel(r, GetValueOrDefaultFromMapIndex<T>(0));
        }
    }
    public class RecordMap<R, T1, T2> : RecordMapBase<R>
    {
        MapAction<R, T1, T2> recordMapDel;
        public RecordMap(MapAction<R, T1, T2> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {    //--------------------------------------------------------------
            //TODO: we can optimize how to map data with dynamic method ***
            //--------------------------------------------------------------

            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1));
        }
    }
    public class RecordMap<R, T1, T2, T3> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {    //--------------------------------------------------------------
            //TODO: we can optimize how to map data with dynamic method ***
            //-------------------------------------------------------------- 
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2)
                );
        }
    }
    public class RecordMap<R, T1, T2, T3, T4> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3, T4> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3, T4> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            //--------------------------------------------------------------
            //TODO: we can optimize how to map data with dynamic method ***
            //--------------------------------------------------------------

            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2),
                GetValueOrDefaultFromMapIndex<T4>(3)
                );
        }
    }
    public class RecordMap<R, T1, T2, T3, T4, T5> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3, T4, T5> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2),
                GetValueOrDefaultFromMapIndex<T4>(3),
                GetValueOrDefaultFromMapIndex<T5>(4)
                );
        }
    }
    public class RecordMap<R, T1, T2, T3, T4, T5, T6> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3, T4, T5, T6> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2),
                GetValueOrDefaultFromMapIndex<T4>(3),
                GetValueOrDefaultFromMapIndex<T5>(4),
                GetValueOrDefaultFromMapIndex<T6>(5)
                );
        }
    }
    public class RecordMap<R, T1, T2, T3, T4, T5, T6, T7> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3, T4, T5, T6, T7> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2),
                GetValueOrDefaultFromMapIndex<T4>(3),
                GetValueOrDefaultFromMapIndex<T5>(4),
                GetValueOrDefaultFromMapIndex<T6>(5),
                GetValueOrDefaultFromMapIndex<T7>(6)
                );
        }
    }
    public class RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2),
                GetValueOrDefaultFromMapIndex<T4>(3),
                GetValueOrDefaultFromMapIndex<T5>(4),
                GetValueOrDefaultFromMapIndex<T6>(5),
                GetValueOrDefaultFromMapIndex<T7>(6),
                GetValueOrDefaultFromMapIndex<T8>(7)
                );
        }
    }
    public class RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2),
                GetValueOrDefaultFromMapIndex<T4>(3),
                GetValueOrDefaultFromMapIndex<T5>(4),
                GetValueOrDefaultFromMapIndex<T6>(5),
                GetValueOrDefaultFromMapIndex<T7>(6),
                GetValueOrDefaultFromMapIndex<T8>(7),
                GetValueOrDefaultFromMapIndex<T9>(9)
                );
        }
    }
    public class RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2),
                GetValueOrDefaultFromMapIndex<T4>(3),
                GetValueOrDefaultFromMapIndex<T5>(4),
                GetValueOrDefaultFromMapIndex<T6>(5),
                GetValueOrDefaultFromMapIndex<T7>(6),
                GetValueOrDefaultFromMapIndex<T8>(7),
                GetValueOrDefaultFromMapIndex<T9>(8),
                GetValueOrDefaultFromMapIndex<T10>(9)
                );
        }
    }
    public class RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2),
                GetValueOrDefaultFromMapIndex<T4>(3),
                GetValueOrDefaultFromMapIndex<T5>(4),
                GetValueOrDefaultFromMapIndex<T6>(5),
                GetValueOrDefaultFromMapIndex<T7>(6),
                GetValueOrDefaultFromMapIndex<T8>(7),
                GetValueOrDefaultFromMapIndex<T9>(8),
                GetValueOrDefaultFromMapIndex<T10>(9),
                GetValueOrDefaultFromMapIndex<T11>(10)
                );
        }
    }
    public class RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2),
                GetValueOrDefaultFromMapIndex<T4>(3),
                GetValueOrDefaultFromMapIndex<T5>(4),
                GetValueOrDefaultFromMapIndex<T6>(5),
                GetValueOrDefaultFromMapIndex<T7>(6),
                GetValueOrDefaultFromMapIndex<T8>(7),
                GetValueOrDefaultFromMapIndex<T9>(8),
                GetValueOrDefaultFromMapIndex<T10>(9),
                GetValueOrDefaultFromMapIndex<T11>(10),
                GetValueOrDefaultFromMapIndex<T12>(11)
                );
        }
    }
    public class RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2),
                GetValueOrDefaultFromMapIndex<T4>(3),
                GetValueOrDefaultFromMapIndex<T5>(4),
                GetValueOrDefaultFromMapIndex<T6>(5),
                GetValueOrDefaultFromMapIndex<T7>(6),
                GetValueOrDefaultFromMapIndex<T8>(7),
                GetValueOrDefaultFromMapIndex<T9>(8),
                GetValueOrDefaultFromMapIndex<T10>(9),
                GetValueOrDefaultFromMapIndex<T11>(10),
                GetValueOrDefaultFromMapIndex<T12>(11),
                GetValueOrDefaultFromMapIndex<T13>(12)
                );
        }
    }
    public class RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2),
                GetValueOrDefaultFromMapIndex<T4>(3),
                GetValueOrDefaultFromMapIndex<T5>(4),
                GetValueOrDefaultFromMapIndex<T6>(5),
                GetValueOrDefaultFromMapIndex<T7>(6),
                GetValueOrDefaultFromMapIndex<T8>(7),
                GetValueOrDefaultFromMapIndex<T9>(8),
                GetValueOrDefaultFromMapIndex<T10>(9),
                GetValueOrDefaultFromMapIndex<T11>(10),
                GetValueOrDefaultFromMapIndex<T12>(11),
                GetValueOrDefaultFromMapIndex<T13>(12),
                GetValueOrDefaultFromMapIndex<T14>(13)
                );
        }
    }
    public class RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> recordMapDel)
            : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1),
                GetValueOrDefaultFromMapIndex<T3>(2),
                GetValueOrDefaultFromMapIndex<T4>(3),
                GetValueOrDefaultFromMapIndex<T5>(4),
                GetValueOrDefaultFromMapIndex<T6>(5),
                GetValueOrDefaultFromMapIndex<T7>(6),
                GetValueOrDefaultFromMapIndex<T8>(7),
                GetValueOrDefaultFromMapIndex<T9>(8),
                GetValueOrDefaultFromMapIndex<T10>(9),
                GetValueOrDefaultFromMapIndex<T11>(10),
                GetValueOrDefaultFromMapIndex<T12>(11),
                GetValueOrDefaultFromMapIndex<T13>(12),
                GetValueOrDefaultFromMapIndex<T14>(13),
                GetValueOrDefaultFromMapIndex<T15>(14)
                );
        }
    }
    public delegate void MapAction<R, T>(
        R r, T t);
    public delegate void MapAction<R, T1, T2>(
        R r, T1 t1, T2 t2);
    public delegate void MapAction<R, T1, T2, T3>(
        R r, T1 t1, T2 t2, T3 t3);
    public delegate void MapAction<R, T1, T2, T3, T4>(
        R r, T1 t1, T2 t2, T3 t3, T4 t4);
    public delegate void MapAction<R, T1, T2, T3, T4, T5>(
        R r, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);
    public delegate void MapAction<R, T1, T2, T3, T4, T5, T6>(
        R r, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);
    public delegate void MapAction<R, T1, T2, T3, T4, T5, T6, T7>(
        R r, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);
    public delegate void MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8>(
        R r, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8);
    //----------------------------------------------------------------------
    public delegate void MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
       R r, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9);
    public delegate void MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
       R r, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10);
    public delegate void MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
       R r, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11);
    public delegate void MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
       R r, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12);
    public delegate void MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
       R r, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13);
    public delegate void MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
       R r, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14);
    public delegate void MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
       R r, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15);
    //----------------------------------------------------------------------



    public static class Mapper
    {
        public static RecordMap<R> Map<R>()
        {
            return new RecordMap<R>();
        }
        public static RecordMap<R, T> Map<R, T>(MapAction<R, T> ac)
        {
            return new RecordMap<R, T>(ac);
        }
        public static RecordMap<R, T1, T2> Map<R, T1, T2>(MapAction<R, T1, T2> ac)
        {
            return new RecordMap<R, T1, T2>(ac);
        }
        public static RecordMap<R, T1, T2, T3> Map<R, T1, T2, T3>(MapAction<R, T1, T2, T3> ac)
        {
            return new RecordMap<R, T1, T2, T3>(ac);
        }
        public static RecordMap<R, T1, T2, T3, T4> Map<R, T1, T2, T3, T4>(MapAction<R, T1, T2, T3, T4> ac)
        {
            return new RecordMap<R, T1, T2, T3, T4>(ac);
        }
        public static RecordMap<R, T1, T2, T3, T4, T5> Map<R, T1, T2, T3, T4, T5>(MapAction<R, T1, T2, T3, T4, T5> ac)
        {
            return new RecordMap<R, T1, T2, T3, T4, T5>(ac);
        }
        public static RecordMap<R, T1, T2, T3, T4, T5, T6> Map<R, T1, T2, T3, T4, T5, T6>(MapAction<R, T1, T2, T3, T4, T5, T6> ac)
        {
            return new RecordMap<R, T1, T2, T3, T4, T5, T6>(ac);
        }
        public static RecordMap<R, T1, T2, T3, T4, T5, T6, T7> Map<R, T1, T2, T3, T4, T5, T6, T7>(MapAction<R, T1, T2, T3, T4, T5, T6, T7> ac)
        {
            return new RecordMap<R, T1, T2, T3, T4, T5, T6, T7>(ac);
        }
        public static RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8> Map<R, T1, T2, T3, T4, T5, T6, T7, T8>(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8> ac)
        {
            return new RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8>(ac);
        }
        public static RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9> Map<R, T1, T2, T3, T4, T5, T6, T7, T8, T9>(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9> ac)
        {
            return new RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ac);
        }
        public static RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Map<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> ac)
        {
            return new RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ac);
        }
        public static RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Map<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> ac)
        {
            return new RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ac);
        }
        public static RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Map<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> ac)
        {
            return new RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ac);
        }
        public static RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
            Map<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> ac)
        {
            return new RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ac);
        }
        public static RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
          Map<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> ac)
        {
            return new RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ac);
        }
        public static RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
          Map<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> ac)
        {
            return new RecordMap<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ac);
        }

    }

    //------------------------------------------------


}