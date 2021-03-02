#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "FamilyFactory.h"



Imperator::Family::Factory::Factory()
{
	registerKeyword("key", [&](std::istream& theStream) {
		family->key = commonItems::getString(theStream);
	});
	registerKeyword("prestige", [&](std::istream& theStream) {
		family->prestige = commonItems::getDouble(theStream);
	});
	registerKeyword("prestige_ratio", [&](std::istream& theStream) {
		family->prestigeRatio = commonItems::getDouble(theStream);
	});
	registerKeyword("culture", [&](std::istream& theStream) {
		family->culture = commonItems::getString(theStream);
	});
	registerKeyword("minor_family", [&](std::istream& theStream) {
		family->minor = commonItems::getString(theStream) == "yes";
	});
	registerKeyword("member", [&](std::istream& theStream) {
		family->members = commonItems::getULlongs(theStream);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}


std::unique_ptr<Imperator::Family> Imperator::Family::Factory::getFamily(std::istream& theStream, const unsigned long long theFamilyID)
{
	family = std::make_unique<Family>();
	family->ID = theFamilyID;

	parseStream(theStream);

	return std::move(family);
}