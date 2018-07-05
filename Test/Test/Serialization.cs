using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Utilities
{
    /// <summary>
    /// 序列化类
    /// </summary>
    public class Serialization
    {
        /// <summary>
        /// 序列化json字符串
        /// </summary>
        /// <typeparam name="SerializationType"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool SerializeJsonObject<SerializationType>(SerializationType obj, out string jsonResult, out string errorMessage)
        {
            jsonResult = "";
            errorMessage = "";
            try
            {
                DataContractJsonSerializer json = new DataContractJsonSerializer(obj.GetType());
                //序列化
                using (MemoryStream ms = new MemoryStream())
                {
                    json.WriteObject(ms, obj);
                    jsonResult = Encoding.UTF8.GetString(ms.ToArray());
                }
                return true;
            }
            catch(Exception e)
            {
                errorMessage = "Serialization SerializeJsonObject Exception : 序列化JSON异常 : " + e.Message + " StackTrace : " + e.StackTrace;
                System.Diagnostics.Trace.TraceError(errorMessage);
                return false;
            }            
        }
        public static bool DeSerializeJsonObject<SerializationType>(string sJson, out SerializationType jsonObject, out string errorMessage)
        {
            jsonObject = default(SerializationType);
            errorMessage = null;
            try
            {
                using (var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(sJson), XmlDictionaryReaderQuotas.Max))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SerializationType));
                    jsonObject = (SerializationType)serializer.ReadObject(jsonReader);
                }
                return true;
            }
            catch (Exception e)
            {
                errorMessage = "Serialization DeSerializeJsonObject Exception : 反序列化JSON异常 : " + e.Message;
                System.Diagnostics.Trace.TraceError(errorMessage);
            }
            return false;
        }


        public class XmlStringWriter : StringWriter
        {
            public XmlStringWriter(Encoding encoding = null)
            {
                if (encoding != null)
                    _encoding = encoding;
            }
            Encoding _encoding = System.Text.Encoding.Default;
            public override Encoding Encoding
            {
                get { return _encoding; }
            }
        }
        /*
        [XmlRoot("MyCity", Namespace="abc.abc", IsNullable=false)]     // 当该类为Xml根节点时，以此为根节点名称。
        public class City
        [XmlAttribute("AreaName")]    // 表现为Xml节点属性。<... AreaName="..."/>
        public string Name
        [XmlElement("AreaId", IsNullable = false)]    // 表现为Xml节点。<AreaId>...</AreaId>
        public string AreaId
        [XmlArray("Areas")]    // 表现为Xml层次结构，根为Areas，其所属的每个该集合节点元素名为类名。<Areas><Area ... /><Area ... /></Areas>
        public Area[] Areas
        [XmlElement("Areas", IsNullable = false)]    // 表现为水平结构的Xml节点。<Area ... /><Area ... />...
        public Area[] Areas
        [XmlIgnoreAttribute]    // 忽略该元素的序列化。
        [XmlText]       //节点InnerText
         */
        /// <summary>
        /// 序列化xml
        /// </summary>
        /// <typeparam name="ObjectType"></typeparam>
        /// <param name="obj"></param>
        /// <param name="content"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public static bool SerializeXmlObject<ObjectType>(ObjectType obj, out string content, out string errorMessage, Encoding encoding = null)
        {
            content = null;
            errorMessage = null;
            try
            {
                using (StringWriter stringWriter = new XmlStringWriter(encoding))
                {
                    using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Indent = true }))    //Indent换行, 缩进
                    {
                        xmlWriter.WriteStartDocument(true); //standalone为yes
                        XmlSerializerNamespaces xmlNameSpaces = new XmlSerializerNamespaces();
                        xmlNameSpaces.Add("", "");  //去掉命名空间
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(ObjectType));
                        xmlSerializer.Serialize(xmlWriter, obj, xmlNameSpaces);
                        content = stringWriter.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return false;
            }
            return true;
        }
        /// <summary>
        /// 反序列化xml
        /// </summary>
        /// <typeparam name="SerializationType"></typeparam>
        /// <param name="xmlContent"></param>
        /// <param name="xmlObject"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public static bool DeserializeXmlObject<SerializationType>(string xmlContent, out SerializationType xmlObject, out string errorMessage)
        {
            xmlObject = default(SerializationType);
            errorMessage = null;
            try
            {
                XmlSerializer xsl = new XmlSerializer(typeof(SerializationType));
                StringReader sr = new StringReader(xmlContent);
                xmlObject = (SerializationType)xsl.Deserialize(sr);
            }
            catch (Exception e)
            {
                errorMessage = "XmlStringWriter DeserializeXmlObject Exception : 反序列化XML异常 : " + e.Message + " StackTrace : " + e.StackTrace + " Xml : " + xmlContent;
                System.Diagnostics.Trace.TraceError(errorMessage);                
                return false;
            }
            return true;
        }
        
    }
}
