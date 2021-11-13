using commonItems;

namespace ImperatorToCK3.CK3.Characters {
	public class CharacterAttributes {
		public int Martial { get; private set; } = 0;
		public int Prowess { get; private set; } = 0;
		public int Diplomacy { get; private set; } = 0;
		public int Intrigue { get; private set; } = 0;
		public int Stewardship { get; private set; } = 0;
		public int Learning { get; private set; } = 0;
		public CharacterAttributes(Imperator.Characters.CharacterAttributes imperatorAttributes) {
			Martial = imperatorAttributes.Martial;
			Prowess = imperatorAttributes.Martial/2 + imperatorAttributes.Finesse/2;
			Diplomacy = imperatorAttributes.Charisma;
			Intrigue = imperatorAttributes.Charisma/2 + imperatorAttributes.Zeal/2;
			Stewardship = imperatorAttributes.Finesse;
			Learning = imperatorAttributes.Zeal;
		}
	}
}
