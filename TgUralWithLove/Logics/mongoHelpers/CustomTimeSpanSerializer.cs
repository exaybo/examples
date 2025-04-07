using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logics.mongoHelpers
{
    using MongoDB.Bson.IO;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization;
    using MongoDB.Bson.Serialization.Serializers;

    public class CustomTimeSpanSerializer : SerializerBase<TimeSpan?>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeSpan? value)
        {
            // Сериализуем TimeSpan в строку
            context.Writer.WriteString(value.ToString());
        }

        public override TimeSpan? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var rawDocument = context.Reader.ReadRawBsonDocument(); // Чтение документа
            using (var reader = new BsonDocumentReader(rawDocument.ToBsonDocument()))
            {
                reader.ReadStartDocument();
                bool isEmpty = reader.CurrentBsonType == BsonType.EndOfDocument;
                reader.ReadEndDocument();
                return null;
            }
        }
    }

}
