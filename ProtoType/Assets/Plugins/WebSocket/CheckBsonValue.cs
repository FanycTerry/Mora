using UnityEngine;
using System.Collections.Generic;
using Kernys.Bson;
using System.Linq;
using System.Text;

public class CheckBsonValue
{
	public static void ShowBsonData (BSONObject obj, string from)
	{
		StringBuilder builder = new StringBuilder ();

		BsonObjectDump (builder, obj, 0);

        builder.Append(AddTime());


        Debug.Log (string.Concat (from, builder.ToString ()));
	}

	public static string GetBsonString (BSONObject obj)
	{
		StringBuilder builder = new StringBuilder ();

		BsonObjectDump (builder, obj, 0);

		return builder.ToString ();
	}

	static void BsonObjectDump (StringBuilder builder, BSONObject obj, int level)
	{
		if (obj == null)
			return;

		string format = "";
		for (var i = 0; i < level; i++)
			format += "  ";
		format += "Bson Key : {0}    \tBson Value : {1}\n";

		string[] keys = obj.Keys.ToArray ();
		//foreach (string s in keys)
		//{
		//    Debug.Log(string.Concat("Key ", s));
		//}
		BSONValue[] values = obj.Values.ToArray ();

		for (int i = 0; i < keys.Length; i++) {
			if (keys [i].Equals ("type")) {
				builder.Insert (0, string.Format (format, keys [i], convertBsonValue (values [i])));
			} else {
				builder.AppendFormat (format, keys [i], convertBsonValue (values [i]));
			}

			if (values [i].valueType == BSONValue.ValueType.Array)
				BsonArrayDump (builder, values [i] as BSONArray, ++level);
			if (values [i].valueType == BSONValue.ValueType.Object) {
				BsonObjectDump (builder, values [i] as BSONObject, ++level);
			}
		}
	}

	static void BsonArrayDump (StringBuilder builder, BSONArray ary, int level)
	{
		for (var i = 0; i < ary.Count; i++) {
			BSONObject bsonObj = ary [i] as BSONObject;

			if (bsonObj != null) {
				BsonObjectDump (builder, bsonObj, level);
			} else {
				BSONValue bsonValue = ary [i] as BSONValue;

				builder.AppendLine (convertBsonValue (bsonValue));
			}

			builder.AppendLine ("\t-------------------------------");
		}
	}

	public static BSONValue ShowBsonDataByIndex (BSONObject obj, int bsonIndex)
	{
		string[] keys = obj.Keys.ToArray ();

		BSONValue[] values = obj.Values.ToArray ();

		if (bsonIndex > obj.Count - 1) {
			Debug.LogError ("Bson Index is Over BsonObject Count , Check again");
			return null;
		}

		Debug.Log (string.Concat ("Bson Key : ", keys [bsonIndex]));

		return values [bsonIndex];
	}

	static string convertBsonValue (BSONValue value)
	{
		string bsonValueStr;

		switch (value.valueType) {
		case BSONValue.ValueType.String:
			bsonValueStr = value.stringValue;
			break;

		case BSONValue.ValueType.Int32:
			bsonValueStr = value.int32Value.ToString ();
			break;

		case BSONValue.ValueType.Int64:
			bsonValueStr = value.int64Value.ToString ();
			break;

        case BSONValue.ValueType.Double:
            bsonValueStr = value.doubleValue.ToString() ;
                break;
       case BSONValue.ValueType.Boolean:
			bsonValueStr = value.boolValue.ToString ();
			break;

		case BSONValue.ValueType.UTCDateTime:
			bsonValueStr = value.dateTimeValue.ToString ();
			break;

		default:
			bsonValueStr = value.valueType.ToString ();
			break;
		}

		return string.Concat ("\t", bsonValueStr);
	}

    /// <summary>
    /// 增加Log的寫入時間.
    /// </summary>
    /// <returns></returns>
    private static string AddTime()
    {
        return string.Format("Time:  {0}",System.DateTime.Now);
    }

}
