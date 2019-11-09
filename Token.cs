using Newtonsoft.Json.Linq;
using System;

namespace MTGText2Pdf
{
    [Serializable]
    public class Token : ImageAble
    {
        public string name;
        public string scryfallId;
        public string scryfallIllustrationId;
        public string scryfallOracleId;
        public string uuid;
        public DateTime releaseDate;
        public string[] imageUrls;
        public string imageFileName;
        public string cachedImage;

        public static Token Parse(JObject data, Set set)
        {
            Token token = new Token();

            token.name = data.ContainsKey("name") ? data["name"].ToString() : "";
            token.scryfallId = data.ContainsKey("scryfallId") ? data["scryfallId"].ToString() : "";
            token.scryfallIllustrationId = data.ContainsKey("scryfallIllustrationId") ? data["scryfallIllustrationId"].ToString() : "";
            token.scryfallOracleId = data.ContainsKey("scryfallOracleId") ? data["scryfallOracleId"].ToString() : "";
            token.uuid = data.ContainsKey("uuid") ? data["uuid"].ToString() : "";
            token.releaseDate = set.releaseDate;

            token.imageUrls = new string[2]
            {
                "https://api.scryfall.com/cards/" + token.scryfallId + "?format=image",
                "http://gatherer.wizards.com/Handlers/Image.ashx?name=" + token.name + "&type=card"
            };

            token.imageFileName = "token--" + set.name + "--" + token.uuid + ".jpg";
            token.cachedImage = "";

            if(token.uuid.Length == 0)
            {
                throw new Exception("Uhh");
            }

            return token;
        }

        public string[] GetImageUrls()
        {
            return imageUrls;
        }

        public string GetName()
        {
            return name;
        }

        public DateTime GetReleaseDate()
        {
            return releaseDate;
        }

        public string GetImageFileName()
        {
            return imageFileName;
        }

        public void SetCachedImage(string file)
        {
            cachedImage = file;
        }
        public string GetCachedImage()
        {
            return cachedImage;
        }
    }
}
