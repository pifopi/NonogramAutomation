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
            { "Poissons, Monde sous-marin", "Fish" },
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
            { "Bateaux, Navires", "Ships" },
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

                await UpdatePuzzleList(BWs);
                string BWsLuaContent = await PuzzleListToLua(BWs);
                System.IO.File.WriteAllText("BWs_cleaned.lua", BWsLuaContent);


                await UpdatePuzzleList(colors);
                string colorsLuaContent = await PuzzleListToLua(colors);
                System.IO.File.WriteAllText("Colors_cleaned.lua", colorsLuaContent);
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
            }
        }

        private async Task UpdatePuzzleList(List<Puzzle> puzzles)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            foreach (Puzzle puzzle in puzzles)
            {
                await GoToSearchMenuAsync(TimeSpan.FromSeconds(10), _token);
                await InputPuzzleAsync(TimeSpan.FromSeconds(10), _token, puzzle.Link);
                await GoToPuzzleListAsync(TimeSpan.FromSeconds(10), _token);
                FoundElement? element = await Utils.FindElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/btnCtxMenu']", TimeSpan.FromSeconds(10), _token);
                if (element is null)
                {
                    UpdateValue(puzzle, "Link", $"{puzzle.Link} - DELETED");
                    continue;
                }
                string size = await ReadSizeAsync();
                UpdateValue(puzzle, "Size", size);
                await element.Element.ClickAsync(_token);
                await Task.Delay(TimeSpan.FromSeconds(1));
                (string author, string category1, string category2) = await ReadDetailsAsync();
                UpdateValue(puzzle, "Author", author);
                UpdateValue(puzzle, "Category1", category1);
                UpdateValue(puzzle, "Category2", category2);
                await Utils.ClickBackButtonAsync(_adbInstance, _token);
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
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(attribute.Value, @"\d+x\d+") ?? throw new Exception("xml size node attribute is not formatted properly");
            return match.Value;
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
            System.Xml.XmlNode? category2Node = xml.SelectSingleNode("//node[@resource-id='com.ucdevs.jcross:id/tag2']");
            if (category2Node is null)
            {
                return "";
            }
            else
            {
                return await ReadCategoryAsync(category2Node) ?? throw new Exception("category 1 is missing");
            }
        }

        private async Task<string?> ReadCategoryAsync(System.Xml.XmlNode node)
        {
            System.Xml.XmlAttributeCollection attributes = node.Attributes ?? throw new Exception("xml category node is missing attributes");
            System.Xml.XmlAttribute attribute = attributes["text"] ?? throw new Exception("xml category node is missing text attribute");
            return _categoryDictionnary.GetValueOrDefault(attribute.Value);
        }

        private async Task<string> PuzzleListToLua(List<Puzzle> puzzles)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("return {");

            foreach (var puzzle in puzzles)
            {
                sb.AppendLine("\t{");
                sb.AppendLine($"\t\tlink            = \"{puzzle.Link.Replace("\"", "\\\"")}\",");
                sb.AppendLine($"\t\tauthor          = \"{puzzle.Author}\",");
                sb.AppendLine($"\t\txp              = \"{puzzle.XP}\",");
                sb.AppendLine($"\t\tnew_xp          = \"{puzzle.NewXP}\",");
                sb.AppendLine($"\t\tsize            = \"{puzzle.Size}\",");
                sb.AppendLine($"\t\tcategory_1      = \"{puzzle.Category1}\",");
                sb.AppendLine($"\t\tcategory_2      = \"{puzzle.Category2}\",");
                sb.AppendLine($"\t\tpuzzle_type     = \"{puzzle.PuzzleType}\",");

                sb.AppendLine("\t},");
            }

            sb.AppendLine("\t-- Placeholder (for copy-pasting).");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\tlink            = \"\",");
            sb.AppendLine($"\t\tauthor          = \"\",");
            sb.AppendLine($"\t\txp              = \"\",");
            sb.AppendLine($"\t\tnew_xp          = \"\",");
            sb.AppendLine($"\t\tsize            = \"\",");
            sb.AppendLine($"\t\tcategory_1      = \"\",");
            sb.AppendLine($"\t\tcategory_2      = \"\",");
            sb.AppendLine($"\t\tpuzzle_type     = \"\",");

            sb.AppendLine("\t},");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}