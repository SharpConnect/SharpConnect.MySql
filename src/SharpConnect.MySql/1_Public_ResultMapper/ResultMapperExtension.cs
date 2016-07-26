//MIT, 2015-2016, brezza92, EngineKit and contributors

using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace SharpConnect.MySql.Mapper
{

    public abstract class RecordMapBase<R>
    {
        MethodInfo metInfo;
        protected MySqlDataReader reader;
        protected bool checkTableDefinition;
        public RecordMapBase(Delegate del)
        {
            this.metInfo = del.GetMethodInfo();
        }
        protected List<MySqlFieldDefinition> arrangedFields = new List<MySqlFieldDefinition>();
        public MySqlDataReader DataReader
        {
            get { return this.reader; }
            set
            {
                this.reader = value;
                checkTableDefinition = false;
            }
        }
        protected U GetValueOrDefaultFromActualIndex<U>(int actualIndex)
        {
            if (actualIndex < 0)
            {
                return default(U);
            }
            else
            {
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
                return (U)reader.GetValue(arrangedFields[mapIndex].FieldIndex);
            }
        }
        public R Map(R r)
        {
            //map data from currrent record in data reader
            if (!checkTableDefinition)
            {
                EvaluateTableDefinition(metInfo);
                checkTableDefinition = true;
            }
            OnMap(r);
            return r;
        }
        protected abstract void OnMap(R r);
        void EvaluateTableDefinition(MethodInfo met)
        {
            //check current table defintioin first ***
            MySqlSubTable subTable = reader.CurrentSubTable;
            var methodPars = met.GetParameters();
            //target method that we need 
            int j = methodPars.Length;//**
            arrangedFields.Clear();
            for (int i = 1; i < j; ++i)
            {
                //get parameter fieldname
                //and type and check proper type conversion
                var methodPar = methodPars[i];
                string parName = methodPar.Name;
                MySqlFieldDefinition fieldDef = subTable.GetFieldDefinition(methodPar.Name);
                if (fieldDef.IsEmpty)
                {
                    //not found this field
                    arrangedFields.Add(new MySqlFieldDefinition());
                }
                else
                {
                    //get index and type
                    arrangedFields.Add(fieldDef);
                }
            }

        }
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
            //after evaluate table definition
            //we can use field map to map value of record and then invoke
            recordMapDel(r, GetValueOrDefaultFromMapIndex<T>(0));
        }
    }
    public class RecordMap<R, T1, T2> : RecordMapBase<R>
    {
        MapAction<R, T1, T2> recordMapDel;
        public RecordMap(MapAction<R, T1, T2> recordMapDel) : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
            recordMapDel(r,
                GetValueOrDefaultFromMapIndex<T1>(0),
                GetValueOrDefaultFromMapIndex<T2>(1));
        }
    }
    public class RecordMap<R, T1, T2, T3> : RecordMapBase<R>
    {
        MapAction<R, T1, T2, T3> recordMapDel;
        public RecordMap(MapAction<R, T1, T2, T3> recordMapDel) : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
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
        public RecordMap(MapAction<R, T1, T2, T3, T4> recordMapDel) : base(recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        protected override void OnMap(R r)
        {
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
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5> recordMapDel) : base(recordMapDel)
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
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6> recordMapDel) : base(recordMapDel)
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
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7> recordMapDel) : base(recordMapDel)
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
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8> recordMapDel) : base(recordMapDel)
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
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9> recordMapDel) : base(recordMapDel)
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
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> recordMapDel) : base(recordMapDel)
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
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> recordMapDel) : base(recordMapDel)
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
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> recordMapDel) : base(recordMapDel)
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
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> recordMapDel) : base(recordMapDel)
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
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> recordMapDel) : base(recordMapDel)
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
        public RecordMap(MapAction<R, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> recordMapDel) : base(recordMapDel)
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

}