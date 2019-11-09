using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MTGText2Pdf
{
    [Serializable]
    public class Set
    {
        public string name;
        public DateTime releaseDate;

        public static Set Parse(string name, JObject data, CardsManager manager)
        {
            Set set = new Set();

            set.name = name;
            if (data.ContainsKey("releaseDate"))
            {
                set.releaseDate = DateTime.ParseExact(data["releaseDate"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            else
            {
                set.releaseDate = DateTime.ParseExact("1970-01-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (data.ContainsKey("cards"))
            {
                int i = 0;
                foreach (var raw_card in data["cards"])
                {
                    Card card = Card.Parse(raw_card.ToObject<JObject>(), set);
                    ++i;
                    manager.Add(card);
                }
            }

            if (data.ContainsKey("tokens"))
            {
                int i = 0;
                foreach (var raw_token in data["tokens"])
                {
                    Token token = Token.Parse(raw_token.ToObject<JObject>(), set);
                    ++i;
                    manager.Add(token);
                }
            }

            return set;
        }
    }
}
