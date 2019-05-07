using MongoDB.Bson.Serialization.Attributes;

namespace GenericBot.Entities
{
    public class Quote
    {
        [BsonId]
        public int Id { get; set; }
        public string Content { get; set; }
        public bool Active { get; set; }

        public Quote()
        {
            Active = true;
        }

        public Quote(string c, int i)
        {
            Content = c;
            Id = i;
            Active = true;
        }

        public override string ToString()
        {
            return $"\"{Content}\" (#{Id})";
        }
    }
}
