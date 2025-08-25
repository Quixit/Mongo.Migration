using System;
using Mongo.Migration.Documents;
using Mongo.Migration.Migrations.Document;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Mongo.Migration.Services.Interceptors
{
    internal class MigrationInterceptor<TDocument> : IBsonSerializer<TDocument>
        where TDocument : class, IDocument
    {
        private readonly IDocumentVersionService _documentVersionService;

        private readonly IDocumentMigrationRunner _migrationRunner;

        private readonly BsonClassMapSerializer<TDocument> _serializer;

        public MigrationInterceptor(IDocumentMigrationRunner migrationRunner, IDocumentVersionService documentVersionService)
            : base()
        {
            this._migrationRunner = migrationRunner;
            this._documentVersionService = documentVersionService;
            
            _serializer = new BsonClassMapSerializer<TDocument>(BsonClassMap.LookupClassMap(typeof(TDocument)));
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TDocument value)
        {
            this._documentVersionService.DetermineVersion(value);

            _serializer.Serialize(context, args, value);
        }

        public TDocument Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            // TODO: Performance? LatestVersion, dont do anything
            var document = BsonDocumentSerializer.Instance.Deserialize(context);

            this._migrationRunner.Run(typeof(TDocument), document);

            var migratedContext =
                BsonDeserializationContext.CreateRoot(new BsonDocumentReader(document));

            return _serializer.Deserialize(migratedContext, args);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value is TDocument)
                Serialize(context, args, (TDocument)value);
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }

        public Type ValueType => typeof(TDocument);
    }
}