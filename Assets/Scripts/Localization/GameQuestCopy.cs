public static class GameQuestCopy
{
    private static readonly string[] Titles={"Lezzetli bir öğün","Taze su zamanı","Tatlı bir uyku","Biraz sevgi","Top peşinde","Patileri ger","Fare avı","Tünel keşfi","Meraklı kedi","Oyuncak zamanı","Kuşları izle","Küçük bir koşu","Evine bir dokunuş","Salıncak keyfi","Duş zamanı","Kâğıt oyunu","Kumda keşif","Bakım zamanı","Sepet keşfi","Aynadaki arkadaş","Yumuşak patiler","Küvet keşfi","Mutfak merakı","Raf keşfi","Arabayı it","Meraklı patiler","Yatak odası keyfi","Bahçe tırmanışı","Bahçeyi izle","Balkon keyfi","Kuş yemliği","Avlu tırmanışı","Avlu keyfi","Üst kat keşfi"};
    private static readonly string[] Actions={"Kedini besle","Kedine su ver","Kedini uyut","Kedini sev","Topla oyna","Tırmalama alanını kullan","Fare yakala","Tünelde oyna","Kitaplık, bitki veya lambaderi izle","Oyuncakla oyna","Kuşları izle","Cat Runner oyna","Bir eşya edin","Salıncakta sallan","Duş alanını kullan","Kâğıtla oyna","Kum kabını kullan","Kedine bakım yap","Sepeti keşfet","Aynaya bak","Minderi yoğur","Küvetin kenarını keşfet","Mutfağı izle","Kiler rafına tırman","Servis arabasını it","Eşyayla etkileşime gir","Yatak odasını keşfet","Bahçede tırman","Bahçeyi izle","Balkonu izle","Yemlikle etkileşime gir","Avluda tırman","Avluyu izle","Üst katı keşfet"};
    public static string Title(QuestType type,string fallback)
    {
        int i=(int)type;return GameLanguageService.Current==GameLanguage.Turkish && i>=0 && i<Titles.Length?Titles[i]:fallback;
    }
    public static string Description(QuestType type,int count,string fallback)
    {
        if(type==QuestType.MouseHunt)return GameContentCopy.Text("Cat Catch'te bir turda en az 3 fare yakala.","Catch at least 3 mice in one Cat Catch round.");
        int i=(int)type;if(GameLanguageService.Current!=GameLanguage.Turkish || i<0 || i>=Actions.Length)return fallback;
        return Actions[i]+(count>1?$" · {count} kez":"")+".";
    }
}
