using AdvancedSharpAdbClient.DeviceCommands;

namespace NonogramAutomation
{
    public class ProgramLua : Program
    {
        class Puzzle
        {
            public required string Link { get; set; }
            public required string Author { get; set; }
            public required string XP { get; set; }
            public required string NewXP { get; set; }
            public required string Size { get; set; }
            public required string Category1 { get; set; }
            public required string Category2 { get; set; }
            public required string PuzzleType { get; set; }
        }

        static Dictionary<string, string> _categoryDictionnary = new()
        {
            // Dark blue color set: 
            { "Kawaii, Chibi, Jouets", "Kawaii" },
            { "Anime, Manga", "Anime" },
            { "Films d'animation, BD", "Cartoon" },
            { "Films, TV", "Movies" },
            { "Jeux vidéo, Pixel Art", "Video" },
            { "Fantasy, Contes de fée", "Fantasy" },
            { "Science Fiction, Steampunk, Cyberpunk", "Fiction" },
            { "Super héros, Êtres iconiques et collectionnables", "Super" },
            { "Créatures fabuleuses, irréelles, Monstres", "Unreal" },
            { "Dragons", "Dragons" },

            // Green color set: 
            { "Animaux", "Animals" },
            { "Chats", "Cats" },
            { "Chiens", "Dogs" },
            { "Oiseaux", "Birds" },
            { "Oiseaux", "Fish" },
            { "Insectes, Papillons, Araignées, Escargots", "Insects" },
            { "Plantes, Légumes, Fruits, Baies, Champignons", "Plants" },
            { "Fleurs", "Flowers" },

            // Blond color set:
            { "People, Portraits, Célébrités", "People" },
            { "Nature, Paysages, Temps", "Nature" },
            { "Peinture, Natures mortes, Abstractions", "Painting" },
            { "Maisons, Bâtiments, Paysage urbain", "Houses" },
            { "Crépuscule, Couchers de soleil", "Twilight" },
            { "Espace", "Space" },
            { "Photoréalisme", "Photorealism" },
            { "Motifs, Designs, Mandalas, Symétrie", "Designs" },

            // Light blue color set:
            { "Dans le monde entier", "Around" },
            { "Égypte", "Egypt" },
            { "Japon", "Japan" },
            { "Aventures, Voyage, Secrets", "Adventures" },
            { "Pirates !", "Pirates" },
            { "Pirates !", "Ships" },
            { "Symboles, Signes, Logo, Glyphes, Armoiries", "Symbols" },
            { "Drapeaux", "Flags" },
            { "Histoire, Culture, Religion", "History" },

            // Purple color set:
            { "Amour, Romantique, Coeurs", "Love" },
            { "Smileys, Emoji, Humeur", "Smiles" },
            { "Horoscope, Tarot, Ésotérisme", "Horoscope" },
            { "Vacances (Noël, Halloween)", "Holidays" },
            { "Divertissement, Humour, Mèmes, Jeux, Hobby", "Games" },
            { "Sport, Extrême", "Sport" },
            { "Musique, Danse", "Music" },

            // Apricot color set:
            { "Nourriture, boissons, bonbons", "Food" },
            { "Objets, Meubles, Maison et Bureau", "Objects" },
            { "Vêtements, Chaussures, Cosmétiques, Bijoux", "Clothes" },
            { "Machines, Électronique", "Machines" },
            { "Transport", "Transport" },
            { "Science, Médecine", "Science" },
            { "Armes, Armée", "Weapons" }
        };

        public ProgramLua(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        public override async Task StartAsync()
        {
            List<Puzzle> BWs = GetPuzzlesFromLua("BWs.lua");
            List<Puzzle> colors = GetPuzzlesFromLua("Colors.lua");
            try
            {
                await _adbInstance.ConnectToInstanceAsync(_token);
                foreach (Puzzle BW in BWs)
                {
                    await GoToSearchMenuAsync(TimeSpan.FromSeconds(10), _token);
                    await InputPuzzleAsync(TimeSpan.FromSeconds(10), _token, BW.Link);
                    await GoToPuzzleListAsync(TimeSpan.FromSeconds(10), _token);
                    string size = await ReadSizeAsync();
                    UpdateValue(BW, "Size", size);
                    await GoToPuzzleDetailsMenuAsync(TimeSpan.FromSeconds(10), _token);
                    (string author, string category1, string category2) = await ReadDetailsAsync();
                    UpdateValue(BW, "Author", author);
                    UpdateValue(BW, "Category1", category1);
                    UpdateValue(BW, "Category2", category2);
                    await Utils.ClickBackButtonAsync(_adbInstance, _token);
                }
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
            }
        }

        private List<Puzzle> GetPuzzlesFromLua(string luaFile)
        {
            MoonSharp.Interpreter.Script lua = new();
            var result = lua.DoFile(luaFile);

            List<Puzzle> puzzles = new();
            foreach (var item in result.Table.Values)
            {
                var table = item.Table;

                string link = table.Get("link").String;
                if (string.IsNullOrEmpty(link))
                {
                    continue;
                }

                string author = table.Get("author").String;
                string xp = table.Get("xp").String;
                string new_xp = table.Get("new_xp").String;
                string size = table.Get("size").String;
                string category_1 = table.Get("category_1").String;
                string category_2 = table.Get("category_2").String;
                string puzzle_type = table.Get("puzzle_type").String;

                puzzles.Add(new Puzzle
                {
                    Link = link,
                    Author = author,
                    XP = xp,
                    NewXP = new_xp,
                    Size = size,
                    Category1 = category_1,
                    Category2 = category_2,
                    PuzzleType = puzzle_type
                });
            }
            return puzzles;
        }

        private async Task GoToSearchMenuAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/action_filter']", timeout, token);
        }

        private async Task InputPuzzleAsync(TimeSpan timeout, CancellationToken parentToken, string puzzle)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/editName']", timeout, linkedCts.Token);
            await _adbInstance.AdbClient.ClearInputAsync(_adbInstance.DeviceData, 10, linkedCts.Token);
            string puzzleId = System.Text.RegularExpressions.Regex.Replace(puzzle, @":.*(?=\])", "");
            await _adbInstance.AdbClient.SendTextAsync(_adbInstance.DeviceData, puzzleId, linkedCts.Token);
            await _adbInstance.AdbClient.ClickBackButtonAsync(_adbInstance.DeviceData, linkedCts.Token);
        }

        private async Task GoToPuzzleListAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/buttonsHolder']/node[3]", timeout, token);
        }

        private async Task<string> ReadSizeAsync()
        {
            System.Xml.XmlDocument xml = await Utils.DumpXMLAsync(_adbInstance, _token) ?? throw new Exception("xml document was null");
            System.Xml.XmlNode sizeNode = xml.SelectSingleNode("//node[@resource-id='com.ucdevs.jcross:id/text']") ?? throw new Exception("xml document is missing size node");
            System.Xml.XmlAttributeCollection attributes = sizeNode.Attributes ?? throw new Exception("xml size node is missing attributes");
            System.Xml.XmlAttribute attribute = attributes["text"] ?? throw new Exception("xml size node is missing text attribute");
            return attribute.Value.Split('\n')[2].Trim();
        }

        private void UpdateValue(Puzzle puzzle, string propertyName, string newValue)
        {
            System.Reflection.PropertyInfo propertyInfo = typeof(Puzzle).GetProperty(propertyName) ?? throw new Exception($"puzzle is missing property {propertyName}");
            string? previousValue = (string?)propertyInfo.GetValue(puzzle);
            if (previousValue != newValue)
            {
                Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"Puzzle {puzzle.Link} is different for {propertyName} ('{previousValue}' vs '{newValue}')");
                propertyInfo.SetValue(puzzle, newValue);
            }
        }

        private async Task GoToPuzzleDetailsMenuAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/btnCtxMenu']", timeout, token);
        }

        private async Task<(string, string, string)> ReadDetailsAsync()
        {
            System.Xml.XmlDocument xml = await Utils.DumpXMLAsync(_adbInstance, _token) ?? throw new Exception("xml document was null");

            string author = await ReadAuthorAsync(xml);
            string category1 = await ReadCategory1Async(xml);
            string category2 = await ReadCategory2Async(xml);

            return (author, category1, category2);
        }

        private async Task<string> ReadAuthorAsync(System.Xml.XmlDocument xml)
        {
            System.Xml.XmlNode authorNode = xml.SelectSingleNode("//node[@resource-id='com.ucdevs.jcross:id/author']") ?? throw new Exception("xml document is missing author node");
            System.Xml.XmlAttributeCollection attributes = authorNode.Attributes ?? throw new Exception("xml author node is missing attributes");
            System.Xml.XmlAttribute attribute = attributes["text"] ?? throw new Exception("xml author node is missing text attribute");
            return attribute.Value.Split('\n')[1].Trim();
        }

        private async Task<string> ReadCategory1Async(System.Xml.XmlDocument xml)
        {
            System.Xml.XmlNode category1Node = xml.SelectSingleNode("//node[@resource-id='com.ucdevs.jcross:id/tag1']") ?? throw new Exception("xml document is missing category 1 node");
            return await ReadCategoryAsync(category1Node) ?? throw new Exception("category 1 is missing");
        }

        private async Task<string> ReadCategory2Async(System.Xml.XmlDocument xml)
        {
            System.Xml.XmlNode category2Node = xml.SelectSingleNode("//node[@resource-id='com.ucdevs.jcross:id/tag2']") ?? throw new Exception("xml document is missing category 2 node");
            string? category2 = await ReadCategoryAsync(category2Node);
            if (category2 is null)
            {
                return "";
            }
            else
            {
                return category2;
            }
        }

        private async Task<string?> ReadCategoryAsync(System.Xml.XmlNode node)
        {
            System.Xml.XmlAttributeCollection attributes = node.Attributes ?? throw new Exception("xml category node is missing attributes");
            System.Xml.XmlAttribute attribute = attributes["text"] ?? throw new Exception("xml category node is missing text attribute");
            return _categoryDictionnary.GetValueOrDefault(attribute.Value);
        }
    }
}