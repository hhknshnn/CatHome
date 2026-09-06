using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using U=PremiumUiElements;

public static class PremiumLeaderboardBuilder
{
    public static LeaderboardPanel Build(Transform parent)
    {
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PremiumUiStyle.PremiumFontAssetPath);
        var root=U.Rect("LeaderboardPanel",parent);U.Fill(root);
        var group=root.gameObject.AddComponent<CanvasGroup>();
        var scrim=root.gameObject.AddComponent<Image>();scrim.color=new Color32(41,58,59,185);
        var click=root.gameObject.AddComponent<Button>();click.targetGraphic=scrim;click.transition=Selectable.Transition.None;
        var safe=U.Rect("SafeArea",root);U.Fill(safe);safe.gameObject.AddComponent<SafeAreaRect>();
        var card=U.Panel("LeaderboardCard",safe,PremiumUiStyle.Ivory,0,0,1460,900,32,true);
        root.gameObject.AddComponent<CatRunnerResponsiveLayout>().EditorConfigure(safe,null,card.rectTransform,null,null,null);
        U.Localize(U.Label("Title",card.transform,font,44,PremiumUiStyle.Ink,-154,366,1040,76),"games.rankings");
        var close=U.Action("Close",card.transform,font,"common.close",PremiumUiStyle.Mint,610,366,132,62,out var label);
        var runner=U.Action("RunnerTab",card.transform,font,null,PremiumUiStyle.Mint,-554,276,244,62,out label);label.text="Cat Runner";
        var catCatch=U.Action("CatchTab",card.transform,font,null,PremiumUiStyle.Mint,-294,276,244,62,out label);label.text="Cat Catch";
        var daily=U.Action("DailyTab",card.transform,font,"ranks.daily",PremiumUiStyle.Mint,184,276,180,62,out label);
        var weekly=U.Action("WeeklyTab",card.transform,font,"ranks.weekly",PremiumUiStyle.Mint,380,276,180,62,out label);
        var all=U.Action("AllTimeTab",card.transform,font,"ranks.all",PremiumUiStyle.Mint,576,276,180,62,out label);
        var list=U.Panel("LeaderboardList",card.transform,PremiumUiStyle.WarmIvory,-216,-47,916,540,24);
        var game=U.Label("SelectedGame",list.transform,font,27,PremiumUiStyle.Ink,-130,220,574,46);
        var total=U.Label("GlobalSummary",list.transform,font,20,PremiumUiStyle.Muted,302,220,244,46,TextAlignmentOptions.Right);
        var period=U.Label("SelectedPeriod",list.transform,font,18,PremiumUiStyle.Muted,0,175,832,38);
        var viewport=U.Rect("RankViewport",list.transform);U.At(viewport,0,-2,832,298);viewport.gameObject.AddComponent<RectMask2D>();
        var content=U.Rect("RankContent",viewport);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
        var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=12;layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.childControlWidth=true;
        var fitter=content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var rows=new TMP_Text[CompetitionRules.MaximumVisibleEntries];
        for(int i=0;i<rows.Length;i++)
        {
            var row=U.Panel("RankRow_"+i,content,PremiumUiStyle.Ivory,0,0,832,58,16);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight=58;
            rows[i]=U.Label("RowLabel",row.transform,font,22,PremiumUiStyle.Ink,0,0,784,48);
            U.Fill(rows[i].rectTransform,20);rows[i].richText=true;
        }
        var scroll=list.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
        var barRect=U.Rect("Scrollbar",list.transform);U.At(barRect,437,-2,8,298);
        var track=barRect.gameObject.AddComponent<Image>();track.color=PremiumUiStyle.Mint;
        var handle=U.Panel("Handle",barRect,PremiumUiStyle.Teal,0,0,8,60,4);U.Fill(handle.rectTransform);
        var bar=barRect.gameObject.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;scroll.verticalScrollbar=bar;
        var empty=U.Label("EmptyState",list.transform,font,26,PremiumUiStyle.Muted,0,0,700,180,TextAlignmentOptions.Center);
        var own=U.Label("OwnRank",list.transform,font,22,PremiumUiStyle.Teal,0,-214,832,62);
        var side=U.Panel("YourCat",card.transform,PremiumUiStyle.Mint,472,-47,404,540,24);
        var portrait=U.Rect("Portrait",side.transform);U.At(portrait,0,169,114,114);portrait.gameObject.AddComponent<Image>();portrait.gameObject.AddComponent<SelectedCatPortrait>();
        var name=U.Label("CatName",side.transform,font,29,PremiumUiStyle.Ink,0,80,342,50,TextAlignmentOptions.Center);
        var connection=U.Label("Connection",side.transform,font,21,PremiumUiStyle.Ink,0,-10,342,98,TextAlignmentOptions.Center);
        U.Localize(U.Label("Privacy",side.transform,font,18,PremiumUiStyle.Muted,0,-117,342,80,TextAlignmentOptions.Center),"ranks.privacy");
        U.Localize(U.Label("Rewards",side.transform,font,18,PremiumUiStyle.Muted,0,-212,342,64,TextAlignmentOptions.Center),"ranks.rewards");
        var status=U.Label("Status",card.transform,font,20,PremiumUiStyle.Muted,-150,-375,1046,70);
        var refresh=U.Action("Refresh",card.transform,font,"ranks.refresh",PremiumUiStyle.Coral,536,-375,276,66,out label);
        var panel=root.gameObject.AddComponent<LeaderboardPanel>();
        panel.EditorConfigure(group,close,runner,catCatch,daily,weekly,all,refresh,name,connection,game,period,total,status,own,new TMP_Text[0],new TMP_Text[0],rows);
        var serialized=new SerializedObject(panel);serialized.FindProperty("emptyStateText").objectReferenceValue=empty;serialized.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }
}
