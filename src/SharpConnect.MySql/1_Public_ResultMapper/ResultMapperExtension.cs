//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql.Internal;
using System.Text;
using System.Reflection;
using SharpConnect;
namespace SharpConnect.MySql.Mapper
{
    public class MapRecord<R, T>
    {
        MapAction<R, T> recordMapDel;
        public MapRecord(MapAction<R, T> recordMapDel)
        {
            this.recordMapDel = recordMapDel;

        }
        public T M1 { get; set; }
        public string GetFieldListString()
        {
            //----------------------------------------
            //create simple selection list
            //TODO: check if we have create the string list or not 
            var originalMethod = recordMapDel.GetMethodInfo();

            //get paraemter list
            var parInfos = originalMethod.GetParameters();
            int j = parInfos.Length;
            StringBuilder stbuilder = new StringBuilder();
            for (int i = 1; i < j; ++i)
            {
                //start from 1***
                var parInfo = parInfos[i];
                if (i > 1)
                {
                    stbuilder.Append(',');
                }
                stbuilder.Append(parInfo.Name);
            }
            return stbuilder.ToString();
        }
    }

    public abstract class MapRecordBase
    {
        public abstract string GetFieldListString();
    }
    public class MapRecord2<T> : MapRecordBase
    {
        MapNew<T> recordMapDel;
        public MapRecord2(MapNew<T> recordMapDel)
        {
            this.recordMapDel = recordMapDel;

        }
        public T M1 { get; set; }
        public override string GetFieldListString()
        {
            //----------------------------------------
            //create simple selection list
            //TODO: check if we have create the string list or not

            var originalMethod = recordMapDel.GetMethodInfo();
            //get paraemter list
            var parInfos = originalMethod.GetParameters();
            int j = parInfos.Length;
            StringBuilder stbuilder = new StringBuilder();
            for (int i = 0; i < j; ++i)
            {
                //start from 0***
                var parInfo = parInfos[i];
                if (i > 0)
                {
                    stbuilder.Append(',');
                }
                stbuilder.Append(parInfo.Name);
            }
            return stbuilder.ToString();
        }
    }



    public class MapRecord<R, T1, T2>
    {
        Action<R, T1, T2> recordMapDel;
        public MapRecord(Action<R, T1, T2> recordMapDel)
        {
            this.recordMapDel = recordMapDel;
        }
        public T1 M1 { get; set; }
        public T2 M2 { get; set; }
    }

    public delegate void MapAction<R, T>(R r, T t);
    public delegate void MapNew<T>(T t);
    public static class Mapper
    {
        public static MapRecord2<T> New<T>(MapNew<T> ac)
        {
            return new MapRecord2<T>(ac);
        }

        //----------------------------------------------------------------------
        public static MapRecord<R, T> Map<R, T>(MapAction<R, T> ac)
        {
            return new MapRecord<R, T>(ac);
        }
        public static MapRecord<R, T1, T2> Map<R, T1, T2>(Action<R, T1, T2> ac)
        {
            return new MapRecord<R, T1, T2>(ac);
        }
    }
    public static class SqlBuild
    {
        public static string Select<T>(string srcName, MapRecord2<T> record)
        {
            //create query string from record
            return "select " +
             record.GetFieldListString() +
             "from " + srcName;
        }
    }
}