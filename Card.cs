using Newtonsoft.Json.Linq;
using System;

namespace MTGText2Pdf
{
    [Serializable]
    public class Card : ImageAble
    {
        public bool hasFoil;
        public bool hasNonFoil;
        public bool isMtgo;
        public bool isPaper;
        public bool isReprint;
        public string layout;
        public int mcmId;
        public int mcmMetaId;
        public int mtgoFoilId;
        public int mtgoId;
        public int mtgstocksId;
        public int multiverseId;
        public string name;
        public string scryfallId;
        public string scryfallIllustrationId;
        public string scryfallOracleId;
        public int tcgplayerProductId;
        public string uuid;
        public DateTime releaseDate;
        public string setName;
        public string[] imageUrls;
        public string imageFileName;
        public string cachedImage;

        public static Card Parse(JObject data, Set set)
        {
            Card card = new Card();

            card.name = data.ContainsKey("name") ? data["name"].ToString() : "";
            card.hasFoil = data.ContainsKey("hasFoil") ? bool.Parse(data["hasFoil"].ToString()) : false;
            card.hasNonFoil = data.ContainsKey("hasNonFoil") ? bool.Parse(data["hasNonFoil"].ToString()) : false;
            card.isMtgo = data.ContainsKey("isMtgo") ? bool.Parse(data["isMtgo"].ToString()) : false;
            card.isPaper = data.ContainsKey("isPaper") ? bool.Parse(data["isPaper"].ToString()) : false;
            card.isReprint = data.ContainsKey("isReprint") ? bool.Parse(data["isReprint"].ToString()) : false;
            card.layout = data.ContainsKey("layout") ? data["layout"].ToString() : "";
            card.mcmId = data.ContainsKey("mcmId") ? int.Parse(data["mcmId"].ToString()) : 0;
            card.mcmMetaId = data.ContainsKey("mcmMetaId") ? int.Parse(data["mcmMetaId"].ToString()) : 0;
            card.mtgoFoilId = data.ContainsKey("mtgoFoilId") ? int.Parse(data["mtgoFoilId"].ToString()) : 0;
            card.mtgoId = data.ContainsKey("mtgoId") ? int.Parse(data["mtgoId"].ToString()) : 0;
            card.mtgstocksId = data.ContainsKey("mtgstocksId") ? int.Parse(data["mtgstocksId"].ToString()) : 0;
            card.multiverseId = data.ContainsKey("multiverseId") ? int.Parse(data["multiverseId"].ToString()) : 0;
            card.scryfallId = data.ContainsKey("scryfallId") ? data["scryfallId"].ToString() : "";
            card.scryfallIllustrationId = data.ContainsKey("scryfallIllustrationId") ? data["scryfallIllustrationId"].ToString() : "";
            card.scryfallOracleId = data.ContainsKey("scryfallOracleId") ? data["scryfallOracleId"].ToString() : "";
            card.tcgplayerProductId = data.ContainsKey("tcgplayerProductId") ? int.Parse(data["tcgplayerProductId"].ToString()) : 0;
            card.uuid = data.ContainsKey("uuid") ? data["uuid"].ToString() : "";
            card.releaseDate = set.releaseDate;
            card.setName = set.name;
            card.cachedImage = "";

            card.imageUrls = new string[4]
            {
                "https://api.scryfall.com/cards/" + card.scryfallId + "?format=image",
                "https://api.scryfall.com/cards/multiverse/" + card.multiverseId.ToString() + "format=image",
                "http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid=" + card.multiverseId.ToString() + "&type=card",
                "http://gatherer.wizards.com/Handlers/Image.ashx?name=" + card.name + "&type=card"
            };

            card.imageFileName = "card--" + card.setName + "--" + card.uuid + ".jpg";

            if (card.uuid.Length == 0)
            {
                throw new Exception("Uhh");
            }

            return card;
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
