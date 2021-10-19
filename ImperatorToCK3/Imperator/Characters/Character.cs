﻿using commonItems;
using ImperatorToCK3.Imperator.Families;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Characters {
	public class Character {
		public Character(ulong id) {
			Id = id;
		}
		public ulong Id { get; } = 0;
		private ulong? parsedCountryId;
		public Countries.Country? Country { get; set; }
		private string culture = string.Empty;
		public string Culture {
			get {
				if (!string.IsNullOrEmpty(culture)) {
					return culture;
				}
				if (family is not null && !string.IsNullOrEmpty(family.Culture)) {
					return family.Culture;
				}
				return culture;
			}
			set => culture = value;
		}
		public string Religion { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string? CustomName { get; set; }

		// Returned value indicates whether a family was linked
		internal bool LinkFamily(Families.Families families, SortedSet<ulong> missingDefinitionsSet) {
			if (parsedFamilyId is null) {
				return false;
			}
			var familyId = (ulong)parsedFamilyId;
			if (families.StoredFamilies.TryGetValue(familyId, out var familyToLink)) {
				Family = familyToLink;
				familyToLink.LinkMember(this);
				return true;
			}

			missingDefinitionsSet.Add(familyId);
			return false;
		}

		public string? Nickname { get; set; }
		public ulong ProvinceId { get; private set; } = 0;
		public Date BirthDate { get; private set; } = new Date(1, 1, 1);
		public Date? DeathDate { get; private set; }
		public bool IsDead => DeathDate is not null;
		public string? DeathReason { get; set; }
		private HashSet<ulong> parsedSpouseIds = new();
		public Dictionary<ulong, Character> Spouses { get; set; } = new();
		public Dictionary<ulong, Character?> Children { get; set; } = new();
		public KeyValuePair<ulong, Character?> Mother { get; set; } = new();
		public KeyValuePair<ulong, Character?> Father { get; set; } = new();
		private ulong? parsedFamilyId;
		private Family? family;
		public Family? Family {
			get => family;
			set {
				if (value is null) {
					Logger.Warn($"Setting null family to character {Id}!");
				}
				family = value;
			}
		}
		public List<string> Traits { get; set; } = new();
		public CharacterAttributes Attributes { get; private set; } = new();
		public uint Age { get; private set; } = new();
		public string? DNA { get; private set; }
		public PortraitData? PortraitData { get; private set; }
		public string AgeSex {
			get {
				if (Age >= 16) {
					if (Female) {
						return "female";
					}
					return "male";
				}
				if (Female) {
					return "girl";
				}
				return "boy";
			}
		}
		public ParadoxBool Female { get; private set; } = new(false);
		public double Wealth { get; private set; } = 0;

		public CK3.Characters.Character? CK3Character { get; set; }

		public void AddYears(int years) {
			BirthDate.ChangeByYears(-years);
		}

		private Genes.GenesDB? genes;

		private static readonly Parser parser = new();
		private static Character parsedCharacter = new(0);
		public static HashSet<string> IgnoredTokens { get; } = new();
		static Character() {
			parser.RegisterKeyword("first_name_loc", reader => {
				var characterName = new CharacterName(reader);
				parsedCharacter.Name = characterName.Name;
				parsedCharacter.CustomName = characterName.CustomName;
			});
			parser.RegisterKeyword("country", reader => {
				parsedCharacter.parsedCountryId = ParserHelpers.GetULong(reader);
			});
			parser.RegisterKeyword("province", reader => {
				parsedCharacter.ProvinceId = ParserHelpers.GetULong(reader);
			});
			parser.RegisterKeyword("culture", reader => {
				parsedCharacter.culture = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("religion", reader => {
				parsedCharacter.Religion = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("female", reader =>
				parsedCharacter.Female = new ParadoxBool(reader)
			);
			parser.RegisterKeyword("traits", reader => {
				parsedCharacter.Traits = ParserHelpers.GetStrings(reader);
			});
			parser.RegisterKeyword("birth_date", reader => {
				var dateStr = ParserHelpers.GetString(reader);
				parsedCharacter.BirthDate = new Date(dateStr, true); // converted to AD
			});
			parser.RegisterKeyword("death_date", reader => {
				var dateStr = ParserHelpers.GetString(reader);
				parsedCharacter.DeathDate = new Date(dateStr, true); // converted to AD
			});
			parser.RegisterKeyword("death", reader => {
				parsedCharacter.DeathReason = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("age", reader => {
				parsedCharacter.Age = (uint)ParserHelpers.GetInt(reader);
			});
			parser.RegisterKeyword("nickname", reader => {
				parsedCharacter.Nickname = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("family", reader => {
				parsedCharacter.parsedFamilyId = ParserHelpers.GetULong(reader);
			});
			parser.RegisterKeyword("dna", reader => {
				parsedCharacter.DNA = ParserHelpers.GetString(reader);
			});
			parser.RegisterKeyword("mother", reader => {
				parsedCharacter.Mother = new(ParserHelpers.GetULong(reader), null);
			});
			parser.RegisterKeyword("father", reader => {
				parsedCharacter.Father = new(ParserHelpers.GetULong(reader), null);
			});
			parser.RegisterKeyword("wealth", reader => {
				parsedCharacter.Wealth = ParserHelpers.GetDouble(reader);
			});
			parser.RegisterKeyword("spouse", reader => {
				parsedCharacter.parsedSpouseIds = ParserHelpers.GetULongs(reader).ToHashSet();
			});
			parser.RegisterKeyword("children", reader => {
				foreach (var child in ParserHelpers.GetULongs(reader)) {
					parsedCharacter.Children.Add(child, null);
				}
			});
			parser.RegisterKeyword("attributes", reader => {
				parsedCharacter.Attributes = CharacterAttributes.Parse(reader);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				IgnoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});
		}
		public static Character Parse(BufferedReader reader, string idString, Genes.GenesDB? genesDB) {
			parsedCharacter = new Character(ulong.Parse(idString)) {
				genes = genesDB
			};

			parser.ParseStream(reader);
			if (parsedCharacter.DNA?.Length == 552) {
				parsedCharacter.PortraitData = new PortraitData(parsedCharacter.DNA, parsedCharacter.genes, parsedCharacter.AgeSex);
			}

			return parsedCharacter;
		}

		// Returns counter of linked spouses
		public int LinkSpouses(Dictionary<ulong, Character> characters) {
			var counter = 0;
			foreach (var spouseId in parsedSpouseIds) {
				if (characters.TryGetValue(spouseId, out var spouseToLink)) {
					Spouses.Add(spouseToLink.Id, spouseToLink);
					++counter;
				} else {
					Logger.Warn($"Spouse ID: {spouseId} has no definition!");
				}
			}

			return counter;
		}

		// Returns whether a country was linked
		public bool LinkCountry(Countries.Countries countries) {
			if (parsedCountryId is null) {
				Logger.Warn($"Character {Id} has no country!");
				return false;
			}
			var countryId = (ulong)parsedCountryId;
			if (countries.StoredCountries.TryGetValue(countryId, out var countryToLink)) {
				Country = countryToLink;
				return false;
			}
			Logger.Warn($"Country with ID {countryId} has no definition!");
			return false;
		}
	}
}
