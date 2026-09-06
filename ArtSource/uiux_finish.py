from pathlib import Path
root=Path(__file__).resolve().parents[1]
def edit(path,old,new):
 p=root/path;s=p.read_text(encoding='utf-8-sig');assert old in s,(path,old[:90]);p.write_text(s.replace(old,new),encoding='utf-8')
def span(path,start,end,new):
 p=root/path;s=p.read_text(encoding='utf-8-sig');a=s.index(start);b=s.index(end,a);p.write_text(s[:a]+new+s[b:],encoding='utf-8')

span('Assets/Editor/CatRunnerContentBuilder.cs','    private static LeaderboardPanel BuildLeaderboardPanel(', '    private static void BuildLeaderboardPodiumCard(', '''    private static LeaderboardPanel BuildLeaderboardPanel(Transform parent)
    {
        return PremiumLeaderboardBuilder.Build(parent);
    }

''')
edit('Assets/Scripts/Localization/GameContentCopy.cs','public static class GameContentCopy\n{','public static class GameContentCopy\n{\n    public static string Text(string turkish,string english) => GameLanguageService.Current == GameLanguage.Turkish ? turkish : english;')
path='Assets/Scripts/Games/LeaderboardPanel.cs'
edit(path,'[SerializeField] private TMP_Text[] rankRows;','[SerializeField] private TMP_Text[] rankRows;\n    [SerializeField] private TMP_Text emptyStateText;')
edit(path,'SetStatus("LOADING RANKINGS...");','SetStatus(GameContentCopy.Text("Sıralama yükleniyor…", "Loading rankings…"));\n        if(emptyStateText!=null) { emptyStateText.gameObject.SetActive(true); emptyStateText.text=GameContentCopy.Text("Oyuncular yükleniyor…", "Finding the players…"); }')
edit(path,'$"REWARDS CLAIMED  +{summary.Coins} COIN  +{summary.Diamonds} DIAMONDS"','GameContentCopy.Text($"Ödüller alındı · {summary.Coins} jeton + {summary.Diamonds} elmas",$"Rewards collected · {summary.Coins} coins + {summary.Diamonds} diamonds")')
span(path,'        int totalPlayers = snapshot?', '        if (ownRankText != null)', '''        int totalPlayers = snapshot?.totalPlayers ?? 0;
        int count = snapshot?.entries?.Count ?? 0;
        if(globalSummaryText!=null)globalSummaryText.text=GameContentCopy.Text($"{totalPlayers:N0} oyuncu",$"{totalPlayers:N0} players");
        if(emptyStateText!=null)
        {
            emptyStateText.gameObject.SetActive(count==0);
            emptyStateText.text=GameContentCopy.Text("Henüz bir skor yok.\\nİlk oyununla listede yerini al.","No scores just yet.\\nPlay a game to join the list.");
        }
        if(rankRows!=null)for(int i=0;i<rankRows.Length;i++)
        {
            var row=rankRows[i];if(row==null)continue;
            var entry=i<count?snapshot.entries[i]:null;
            row.transform.parent.gameObject.SetActive(entry!=null);
            if(entry==null)continue;
            // Names are user data: escape TMP markup before adding alignment tags.
            var nickname=(entry.nickname??string.Empty).Replace("<","‹").Replace(">","›");
            row.text=$"#{entry.rank}<pos=13%>{nickname}<pos=78%>{entry.score:N0}";
            row.color=entry.isCurrentPlayer?PremiumUiStyle.Teal:PremiumUiStyle.Ink;
        }

''')
for old,new in {
'"NAME YOUR CAT TO JOIN"':'GameContentCopy.Text("Katılmak için kedine isim ver", "Name your cat to join")',
'"YOUR RANK  —     PLAY TO JOIN"':'GameContentCopy.Text("Sıran · İlk oyununla katıl", "Your rank · Play to join")',
'$"YOUR RANK  #{own.rank}     {own.score:N0} PTS"':'GameContentCopy.Text($"Sıran #{own.rank} · {own.score:N0} puan",$"Your rank #{own.rank} · {own.score:N0} points")',
'"CAT NAME MUST USE 3-14 LETTERS OR NUMBERS"':'GameContentCopy.Text("Kedi adı 3–14 harf veya rakam içermeli.", "Cat names need 3–14 letters or numbers.")',
'"OFFLINE COPY — WILL REFRESH WHEN CONNECTED"':'GameContentCopy.Text("Kaydedilmiş liste · Bağlanınca yenilenir", "Saved list · Refreshes when connected")',
'"DAILY — RESETS 00:00 UTC"':'GameContentCopy.Text("Her gün 00.00 UTC’de yenilenir", "Resets daily at 00:00 UTC")',
'"WEEKLY — RESETS MONDAY"':'GameContentCopy.Text("Pazartesi yenilenir", "Resets on Monday")',
'"ALL-TIME"':'GameContentCopy.Text("Tüm zamanlar", "All time")',
'"NAME YOUR CAT"':'GameContentCopy.Text("Kedine isim ver", "Name your cat")',
'catName.ToUpperInvariant()':'catName',
'"GOOGLE CONNECTED\\nRUN SCORES SUBMIT AUTOMATICALLY"':'GameContentCopy.Text("Google bağlı\\nSkorların otomatik kaydedilir.", "Google connected\\nScores are submitted automatically.")',
'"CONNECT GOOGLE TO\\nSUBMIT SCORES & CLAIM REWARDS"':'GameContentCopy.Text("Skorlarını paylaşmak için ayarlardan Google hesabını bağla.", "Connect Google in Settings to submit scores and claim rewards.")'
}.items():edit(path,old,new)
edit(path,'    private void RefreshHeaders()\n    {','''    private void RefreshHeaders()
    {
        Paint(runnerButton,game==CompetitionGame.CatRunner);Paint(catchButton,game==CompetitionGame.CatCatch);
        Paint(dailyButton,period==CompetitionPeriod.Daily);Paint(weeklyButton,period==CompetitionPeriod.Weekly);Paint(allTimeButton,period==CompetitionPeriod.AllTime);''')
edit(path,'    private void SetControls(bool enabled)','''    private static void Paint(Button button,bool selected)
    {
        if(button==null)return;
        var panel=button.targetGraphic as LowPolyPanelGraphic;
        if(panel!=null)panel.SetPremiumBaseColor(selected?PremiumUiStyle.Teal:PremiumUiStyle.Mint);
        var label=button.GetComponentInChildren<TMP_Text>(true);if(label!=null)label.color=selected?Color.white:PremiumUiStyle.Ink;
    }

    private void SetControls(bool enabled)''')
# Localized static UI labels.
path='Assets/Scripts/Localization/GameLanguageService.cs'
english={'ranks.daily':'Daily','ranks.weekly':'Weekly','ranks.all':'All time','ranks.refresh':'Refresh','ranks.privacy':'Only your cat’s name appears. Your email stays private.','ranks.rewards':'Daily and weekly prizes are added after results are verified.','catch.hint':'Tap a mouse · Your cat chases and pounces','games.time':'Time left','catch.mice':'Mice · Coins'}
turkish={'ranks.daily':'Günlük','ranks.weekly':'Haftalık','ranks.all':'Tüm zamanlar','ranks.refresh':'Yenile','ranks.privacy':'Yalnızca kedinin adı görünür. E-posta adresin gizli kalır.','ranks.rewards':'Günlük ve haftalık ödüller doğrulanan sonuçlarla eklenir.','catch.hint':'Bir fareye dokun · Kedin kovalayıp yakalasın','games.time':'Kalan süre','catch.mice':'Fare · Jeton'}
for lang,data in [('English',english),('Turkish',turkish)]:
 mark=f'private static readonly Dictionary<string, string> {lang} = new()\n    {{'
 edit(path,mark,mark+'\n'+''.join(f'        ["{k}"] = "{v}",\n' for k,v in data.items()))
# Preserve new-best meaning on a tie.
edit('Assets/Scripts/Catch/CatCatchGameController.cs','        if (score > bestScore)\n            bestScore = score;','        bool newBest = score > bestScore;\n        if (newBest)\n            bestScore = score;')
edit('Assets/Scripts/Catch/CatCatchGameController.cs','score>0 && score>=bestScore','newBest')
for path,pairs in {
'Assets/Scripts/Catch/CatCatchGameController.cs':{
'"TAP A SQUEAKY MOUSE\\nYOUR CAT RUNS IT DOWN"':'GameContentCopy.Text("Bir fareye dokun\\nKedin peşinden koşsun", "Tap a squeaky mouse\\nYour cat chases it")',
'"CLOSE IN AND THE CAT POUNCES\\nTHE LANDING IS THE CATCH"':'GameContentCopy.Text("Yaklaşınca kedin atlar\\nİnişte fareyi yakalar", "Get close and your cat pounces\\nLand to catch the mouse")',
'"FILL THE 60 SECONDS\\nCATCH AS MANY AS YOU CAN"':'GameContentCopy.Text("Altmış saniyen var\\nKaç fare yakalayabilirsin?", "You have sixty seconds\\nHow many can you catch?")'},
'Assets/Scripts/Runner/CatRunnerGameController.cs':{
'"DRAG LEFT OR RIGHT\\nCHANGE LANES"':'GameContentCopy.Text("Sağa veya sola sürükle\\nŞerit değiştir", "Drag left or right\\nChange lanes")',
'"SWIPE UP\\nJUMP OVER OBSTACLES"':'GameContentCopy.Text("Yukarı kaydır\\nEngellerin üzerinden atla", "Swipe up\\nJump over obstacles")',
'"SWIPE DOWN\\nSLIDE • FAST-DROP IN AIR"':'GameContentCopy.Text("Aşağı kaydır\\nKayarak geç veya hızlı in", "Swipe down\\nSlide or drop from a jump")',
'$"STAGE {CurrentCurtainNumber}   •   HAPPY CAT +{CatRunnerSessionContext.CareBonusPercent}%"':'GameContentCopy.Text($"Etap {CurrentCurtainNumber} · Mutluluk +%{CatRunnerSessionContext.CareBonusPercent}",$"Stage {CurrentCurtainNumber} · Happiness +{CatRunnerSessionContext.CareBonusPercent}%")',
'$"CHANCES  {ChancesRemaining}/{MaximumCollisionHits}"':'GameContentCopy.Text($"Hak {ChancesRemaining}/{MaximumCollisionHits}",$"Chances {ChancesRemaining}/{MaximumCollisionHits}")',
'$"SCORE  {CurrentScore:N0}"':'GameContentCopy.Text($"Skor {CurrentScore:N0}",$"Score {CurrentScore:N0}")'}
}.items():
 for old,new in pairs.items():edit(path,old,new)

# A real HD scene photograph for Catch. Keep the historical illustration untouched.
edit('Assets/Editor/CatCatchContentBuilder.cs','if (AssetDatabase.LoadAssetAtPath<Texture2D>(CatchHeroPath) == null)\n            BakeWelcomeHero(root.transform, camera, player, mice);','BakeWelcomeHero(root.transform, camera, player, mice);')
span('Assets/Editor/CatCatchContentBuilder.cs','            EnsureFolder("Assets/Art/Catch/UI");','            RenderSettings.ambientMode = AmbientMode.Flat;','            EnsureFolder("Assets/Art/Games");\n')
edit('Assets/Editor/CatCatchContentBuilder.cs','new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32)','new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32)')
edit('Assets/Editor/CatCatchContentBuilder.cs','System.IO.File.WriteAllBytes(CatchHeroPath, baked.EncodeToPNG());','System.IO.File.WriteAllBytes("Assets/Art/Games/CatchPreview.png", baked.EncodeToPNG());')
edit('Assets/Editor/CatCatchContentBuilder.cs','AssetDatabase.ImportAsset(CatchHeroPath, ImportAssetOptions.ForceUpdate);','AssetDatabase.ImportAsset("Assets/Art/Games/CatchPreview.png", ImportAssetOptions.ForceUpdate);')
print('Applied leaderboard, localization and game flow refinement.')
