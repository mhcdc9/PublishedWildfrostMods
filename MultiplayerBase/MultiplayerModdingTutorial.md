# A Modder's Crash Course to the Multiplayer Base Mod (MBM)
Michael Coopman

## Introduction

As the name implies, the *Multiplayer Base Mod* (MBM) is a mod for Wildfrost that adds multiplayer functionality. It adds several ways to see what other players are doing in their games whilst you are playing your own. If you are reading this, then the Multiplayer Base Mod (MBM) must be stable enough to be released in some capacity (Yay!). This is great, but no doubt there is still much left to be done before a full release. In addition, there should be at least two that are as available as MBM: the *Multiplayer Pack Mod* (credits to Josh) and the *Sync Mod*. 

The goal of this document is two-fold. The first goal is to explain the parts of MBM that will be most relevant to those who want to make multiplayer mods. MBM is a framework mod; it does not add any additional content by itself. It wants to be built up from, and this document should help streamline the process of understanding it. The second goal is to direct those same people towards what I believe are the parts most resistant to change. MBM will be modified as time goes on, and that affects anything dependent on it. If a method is listed in this document, then I will have a strong aversion to change it in the future (though maybe not forever).

Each chapter/part will focus on a specific class (and possibly some satellite classes as well).  

`Noteworthy methods will be written in code and given its own line like this.`  

Any method denoted with an asterisk `*` might be less relevant to you, possibly because it is called implicitly elsewhere or the functionality is rather specific. Any method denoted with two asterisks `**` are ones that I have not found a use for, but may have a relevance to someone. All other methods are important in their own way. 

At the end of each chapter is an exercise. I recommend trying them out yourself, but a solution outline is provided.

---

### Contact

If you think there is a feature that MBM should have or there is a mistake in this document, let me know. I frequent the Wildfrost Discord as **@Michael C**; feel free to ping me.

---

### Checklist

Below is an outline of the document on what you should get out of it.

---

**Chapter 1: The `HandlerSystem` class** 
- Basic sending/receiving of messages between players
- Defining a handler and a handler routine
- Using `StatusEffectInstantMessage` to send messages.

**Chapter 2: The `CardEncoder` class**
- Transferring cards between players
- Encoding/Decoding CardData/Entities
- Modifiying CardData/Entities as strings

**Chapter 3: `HandlerBattle` and `PlayActions`safe**
- Queueing up `PlayAction`s through `HandlerBattle`
- Sending a card to another player to be played
- Sending a card to another player to add to their hand

**Chapter 4: Miscellaneous**
- Selecting a card in the battle viewer to play
- Displaying a message via `MultTextManager`

---

## Chapter 0: Setup
 
 I will assume that you have been modding Wildfrost for quite a bit of time. I will try to provide examples, but I will not jump through every hoop just for a small example. I want to talk about the new things here, and we have the github tutorials and the discord community for the other stuff.

### Referencing the dll's

***Steamworks***  
MBM is build off the Steamworks API, so you need to reference that to have access to the `Friend` class. The dll (Steamworks.dll) is located in the same folder as all the other dlls from *Basic Project Setup* in the GitHub tutorials. It was not a necessary one, so make sure that you have it in your project.

***Multiplayer Base Mod***  
Subscribe to the mod and find the address where Steam has downloaded it (Easiest way is to open the game, find MBM, and click the folder icon (If you have Mod Stabilizer, you may also need to right-click to see the file button)). Copy the address to the dll. In your preferred IDE of choice, simply add the reference to your project (In Visual Studios, this can be done by right-clicking the project icon and selecting "Add References"). Once you have done that, you have access to the MBM classes.

## Chapter 1: The `HandlerSystem` Class

The `HandlerSystem` class is the backbone of the mod. It is the class in charge of sending, receiving, and delegating messages, which is of utmost importance for any multiplayer functionality. The class has some extra functionality, but the only methods important to you would be sending/receiving messages. Fortunately, the methods are fairly straightforward. (Tip: I usually type `using Net = MultiplayerBase.Handlers.HandlerSystem;` at the top of classes because `HandlerSystem` is a bit long. Just a word of warning if you want to read sync code).

### The Receiving Side

***Defining a handler***  
When `HandlerSystem` receives a message, it will try to find the right method to read it based on its *handler*. All messages transmitted have a "handler" string at the beginning to designate where it needs to go and who should read it. The handlers used in MBM are "CHT", "BAT", "EVE", "INS", "MSC", and "MAP". The sync mod uses one handler: "SYNC". Mods can use as few or as many handlers as they want, as long as they do not overlap. Preferably, make them unique enough to not be replicated (something similar to the GUID perhaps?). The only rule is **do not use vertical bars "|" in your handler**. The `HandlerSystem` uses that as a delimiter. To set up your handler, you need the following line in your `Load` method of the main mod class:

```C#
//Somewhere in your Load method
HandlerSystem.HandlerRoutines.Add("MY_HANDLER", MyHandlerRoutine);
//DO NOT USE VERTICAL BARS "|" IN THE FIRST ARGUMENT

//Somewhere in your Unload method
HandlerSystem.HandlerRoutines.RemoveKey("MY_HANDLER");
```

(Expect the corresponding `Remove` line in `Unload` as well)

This line says that all messages beginning with "MY_HANDLER" will be routed to the method `MyHandlerRoutine` (Please change these names when working on a real mod). Depending on your mod, you may expect different types of messages going through `MyHandlerRoutine`. I usually set these methods as a glorified switch statement (a dictionary is approximately the same result as well). This is the general form I end up with.

---

```C#
//Note: the Friend class is from Steamworks.dll. Make sure you've added that as a reference.
public void MyHandlerRoutine(Friend f, string message) //The handler is not included in this message.
{
	string[] m = HandlerSystem.DecodeMessage(message); //Method explained later
	Debug.Log($"[Multiplayer] {message}"); //In case of tedious debugging
	switch(m[0])
	{
		case "JUNK":
			DoSomething1(f,m);
			break;
		case "GEARHAMMER":
			DoSomething2(f,m);
			break;
		//etc...
	}
}
```

---

### The Sending Side

The sending side consists of `HandlerSystem.SendMessage`, its variants, and some helper functions. Here are all the variants.

***Sending Messages***  
`HandlerSystem.SendMessage(string handler, Friend to, string message, string feedback = null)`  
`HandlerSystem.SendMessageToAll(string handler, string message, bool includeSelf, string feedback = null)`*  
`HandlerSystem.SendMessageToAllOthers(string handler, string message, string feedback = null)`  
`HandlerSystem.SendMessageToRandom(string handler, string message, bool includeSelf, string feedback = null)`

**Note: you must finalize the party in order to send messages, even to yourself.** Using any method will send your message to someone, where it will be run through the desired handler method. The methods only differ in who they send to. If you want to send a message to yourself (maybe for debugging), set `to` to `HandlerSystem.self`. For other players, a list of them will be in `HandlerSystem.friends` (though I doubt you would need it explicitly). The `feedback` argument will display the string to the right of that friend's icon (default is nothing displayed).

---

What's usually going to happen is that messages end up being a sequence of keywords that takes your handler method to the line of code it needs to be in. The `HandlerSystem` class has some methods to do exactly that. Use`ConcatMessage` and/or `AppendTo` to pack seperate strings together and `DecodeMessage` to split them apart.

***Combining/Splitting Basic Messages***  
`string HandlerSystem.ConcatMessage(bool performReplacement, params string[] messages)`  
`string HandlerSystem.AppendTo(string original, string addOn, bool performReplacement = true)`**  
`string[] HandlerSystem.DecodeMessage(string message)`

`ConcatMessage` and `AppendTo` work by adding a delimiter to separate different strings (as of writing, it uses "! "). If you are sending user-data, a user may break the system by putting that delimiter. As a coutermeasure, if `performReplacement` is set to true, then it will perform a pre-emptive replacement to avoid that disaster (as of writing, it is "!" to "!:". `DecodeMessage` undoes this automatically. 

```C#
//Example of concatenating messages
string A = "Hello World";
string B = "Row! Row! Fight The Powa!";
string C = HandlerSystem.ConcatMessage(false, A, B);
string D = HandlerSystem.ConcatMessage(true, A, B);
//C is "Hello World! Row! Row! Fight The Powa!"
//D is "Hello World! Row!: Row!: Fight The Powa!:"

string[] C_split = HandlerSystem.DecodeMessage(C);
string[] D_split = HandlerSystem.DecodeMessage(D);
//C_split ends up as ["Hello World", "Row", "Row", "Fight The Powa"]
//D_split ends up as ["Hello World", "Row! Row! Fight The Powa]
//4 items vs 2 items.

```

---

### Bonus: The `StatusEffectInstantMessage` Class

A general status class has been made to send messages. It extends the `StatusEffectInstant` class, so do not place it as a card's starting effect; it should be the "X" in ApplyX effects. Here are the variables the class contains.
- `string handler` is the handler for the message.
- `string[] parts` are the various substrings that will be concatenated together. If there is only one part, the concatenation is skipped.
- `ToWhom toWhom` specifies the receiver of the message. The choices are `Self`, `All`, `Random`, `Select`, `Custom`, `Misc`. 
  - `Select` provides the player with a choice on who to send *when the effect resolves.*
  - `Custom` and `Misc` are unimplemented. There are there in case someone extends this class.
- `bool includeSelf` specifies whether include the sending player for `All`, `Random`, and `Select` designations. 
- `bool performReplacement` performs the replacement as described in `SendMessage`.
- `string feedback` displays a message on sned as described in `SendMessage`.
- `ScriptableAmount amount` calculates an amount to possibly send in the message.

In addition, there are some replacement subsctrings. Inputting these will be replaced as follows.
- `{0}`: number of stacks of this status effect (ignoring boosts).
- `{1}`: the target, encoded as a string (see next chapter).
- `{2}`: the id of the card.
- `{3}`: the name of the status effect.
- `{4}`: the output of `amount.Get(target)` or 0 otherwise.

This class is not meant to encapture every use of messages in a status effect. Feel free to make your own message class or extend this class.

---

## Exercise 1: Quantum Folby

***Problem Statement***  
We know enough to do something interesting. Let's make a card similar to Folby but with the effect "When hit, add a Junk to a random player's deck". Try it for yourself or read below for an outline of how it can be done. 

---

***Solution Outline***  
The sequence of events would look something like the following. 
 
(Sender's Game)
1. Folby gets hit.
2. An effect occurs (`ApplyXWhenHit`, where X is an instant message effect).
3. A message is sent to a random player (`SendMessageToRandom`).

(Receiver's Game)

4. The message is read and delegated to the right handler. (e.g. "MY_HANDLER")
5. A Junk is added to their deck.
---

Step 1 does not require any coding. Step 2 requires making two new effects. The first is an `ApplyXWhenHit` that is similar to Folby's original effect except for its `effectToApply`. The second effect is an instance of a new class. A variant of this is provided below.

<details>

```C#
//In CreateModAssets
assets.Add(new StatusEffectDataBuilder(this)
	.Create<StatusEffectInstantMessage>("Instant Give Random Player Junk")
	.SubscribeToAfterAllBuildEvent<StatusEffectInstantMessage>(
	data =>
	{
		data.handler = "MY_HANDLER";
		data.parts = new string[] {"JUNK"}; 
		//"{3}" would be a better choice instead of "JUNK" so that this status effect can be reused, but I wrote "Junk" already.
		data.includeSelf = true; //Why not?
		data.toWhom = StatusEffectInstantMessage.ToWhom.Random;
		data.feedback = "Sending...";
	}));
```

</details>

I'll leave you with making the necessary code for the `ApplyXWhenHit` effect (check the Discord modding channels and/or the pinned tutorials there if you have troubles). Step 3 is handled by the code above. Step 4 requires us to include that necessary line from way above (in *defining a handler*). Finally, step 5 requires editing our handler routine. If you notice in the code block for `MyHandlerRoutine`, we already set the "JUNK" to perform the `DoSomething1` method. Here is what that `DoSomething1` would do. 

```C#
//We don't actually need either argument in this method since the action is so straightforward.
public void DoSomething1(Friend f, string[] messages)
{
	 
	CardData junk = Get<CardData>("Junk")?.Clone();
	if (junk != null)
	{
		References.PlayerData?.inventory?.Add(junk);
	}
}
//This method is overly safe.
//It is important to exercise a bit of caution since we don't know where the other player is right now (have they even started a run yet?).
```
All that's left is to make a Quantum Folby card and give them the `ApplyXWhenHit` effect, then test it out. 

---

## Chapter 2: The `CardEncoder` class

Now that you know how to send/receive messages, you can do anything (with enough effort). The first major obstacle you will come across is this:

**How do you send a card as a string?**

With just a name, `Get` and `TryGet` can fetch a clean copy of `CardData`, but that's a bit inauthentic. During a run `CardData`s are burdened by charms, crowns, and the blood of their enemies. If you want to send a copy from your current battle, then you need to deal with an `Entity`, which could have more effects than its `CardData` (and likely less health).

The `CardEncoder` class has already defined a protocol for encoding/decoding `CardData`s and `Entity`s. The information it stores is roughly the same as if you saved the game mid-battle and reloaded with some exceptions. The only entries of `customData` that are saved are those that are strings and ints. Splatter surfaces are also not maintained (alas, no blood of enemies). There are two methods available for card encoding. The version with the `Entity` argument will hold more information than the `CardData` variant.

`string CardEncoder.Encode(CardData cardData)`  
`string CardEncoder.Encode(Entity entity)`

***Hacky Modifications***  
Since these strings hold the essence of the card, editing the strings can change the cards on the receiver side. MBM provides some methods to help with that.

`void CardEncoder.Modify(ref string s, Action <string[]> modifications)`  
`List<(string, int)> CardEncoder.DecodeToStacks(string s)`
`string CardEncoder.EncodeToStacks(IEnumerable<(string, int)> list)`
`string CardEncoder.EncodeToStacks(IEnumerable<string> list)

Sometimes, just `string.Replace` will suffice for your needs, but these methods are there if you need it. `Modify` takes the string, breaks it into parts, and runs whatever `modifications` method before putting it back together. Using this does require knowing how the card strings are organized. The list below should help with that.

```
//The string[] passed into modifications is as follows
(CardData and Entity encodings)
[0] = internal cardData name
[1] = CustomData (strings and ints)
[2] = attackEffects (should DecodeToStacks first)
[3] = startWithEffects (should DecodeToStacks first)
[4] = traits (should DecodeToStacks first)
[5] = injuries (should DecodeToStacks first... but there's usually only one)
[6] = hp
[7] = damage
[8] = counter
[9] = upgrades
[10] = nickname (player-defined)
(Entity-only encodings)
[11] = height
[12] = max damage
[13] = current damage
[14] = max hp
[15] = current hp
[16] = max counter
[17] = current counter
[18] = effect boost (additive)
[19] = effect boost (multiplicative)
```

attackEffects, startWithEffects, and traits have an additional encoding attached to them. Using `DecodeToStacks` will convert it to an easier to deal with form. After editing it, be sure to use `EncodeToStack` to get a string, then replace the corresponding entry in the array with this new string.

***Decoding cards***  
`CardData CardEncoder.DecodeData(string[] messages, CardData data = null)`
`Entity CardEncoder.DecodeEntity1(CardController cc, Character owner, string[] messages)`*
`IEnumerator CardEncoder.DecodeEntity2(Entity entity, string[] messages)`*
`IEnumerator CardEncoder.CreateAndPlaceEntity(CardController c, CardContainer container, string[] messages)`**

How you decode the string is up to you. If you only need the CardData, use `DecodeData`. This is more suited for manipulating the deck. Note that `DecodeData` can be used on an entity-encoded string (that is, the "wrong" encoding).

If you need the Entity (say for battle), use **BOTH** `DecodeEntity1` and `DecodeEntity2`. The only reason it is split up is because (1) the method ideally should return the entity and (2) the method must be a `Coroutine/IEnumerator` (Yes, there is a way to do both in one method, but it requires lines that are worse to read in my opinion). The `CreateAndPlaceEntity` method exists if you truly do not want to do anything with the entity after making it. This is what decoding an entity generally looks like.

```
//Say I have a string[] with the message.
Entity entity = CardEncoder.DecodeEntity1( null, References.Player, message);
//If the card will actually be used in battle, null should probably be Battle.instance.playerController.
yield return CardEncoder.DecodeEntity2(entity, messages);
```

Just like `DecodeData`, you can decode the "wrong" (CardData) encoding with this message. As you will see in the next chapter, much of the common uses for decoding an entity has been streamlined.

---

## Exercise 2: Bug Maze

Are you familiar with the chess-like game played using two boards where you give captured pieces to your partner to place? Well, this Wildfrost version will be something similar. Whenever a companion dies on one player's board, the companion will leave their deck and move to the deck of another player... with a small health penalty (-1 max hp). Try it for yourself and good luck!

---

Bug Maze sounds like it should be a modifier system (Tutorial 1). Simply hook a `TransferOnDeath` method on `Events.OnEntityKilled` somewhere in `Load` (and unhook on `Unload`). This method will (1) remove the card and (2) encode the card with 1 less max hp.

```C#
public void TransferOnDeath(Entity entity, DeathType type)
{
	if (entity == null) {return;} //Just in case
	
	if (entity.cardType.name == "Friendly")
	{
		CardData data = References.PlayerData.inventory.deck.FirstOrDefault( c =>
		{
			c.id == entity.data.id;
		}); //This may not work right under save/reload, but there are more sophisticated ways.
		if (data != null)
		{
			string s = CardEncoder.Encode(data);
			CardEncoder.Modify(s, (string[] array) =>
			{
				int maxhp = int.Parse(array[6]);
				if (maxhp > 1)
				{
					array[6] = (maxhp - 1).ToString();
				}
			}
			string fullMessage = HandlerSystem.ConcatMessage(true, "ADDTODECK", s);
			//setting performReplacement to true means the card string is considered a single item until it gets decoded a second time (see next code block).
			HandlerSystem.SendMessageToRandom("MyHandler", fullMessage, false, "Sending...");
			References.PlayerData.inventory.deck.Remove(data);
		}
	}
}
```

Then, make a new case for "ADDTODECK" in the `MyHandlerRoutine` method. Something like

```C#
//Inside MyHandlerRoutine method
//...
		case "ADDTODECK":
			AddCardToDeck(f,m);
			break;
//...
//======================================

public void AddCardToDeck(Friend f, string[] m)
{
	string[] cardString = HandlerSystem.DecodeMessage(m[1]);
	CardData data = CardEncoder.DecodeData(cardString);
	//Alternatively, not performing the replacement in ConcatMessage meant passing m.Skip(1).ToArray() instead.
	if (MissingCardSystem.IsMissing(data))
	{
		return;
	}
	References.PlayerData?.inventory?.deck?.Add(data);
}
	
```

---


## Chapter 3: `HandlerBattle` and `PlayAction`'s

This is the most eventful chapter: the one where we can mess with other players while they are actually playing the game. This is also the chapter that can be the most error-prone if you are not following protocol. 

Since we are dealing with battles, it makes sense to be using the `HandlerBattle` class, which handles all messages of MBM involving battles and the battle viewer. In addition, a couple of new `PlayAction`s are in MBM to help with some common animation/sequencing.

***Respect the ActionQueue***  
Most actions (card movement, applying statuses, triggering units) that occur in a battle are coroutines/IEnumerators. So, many of them will take several frames to complete. In order to prevent errors from mistimed interactions, the `ActionQueue` is used to sequence these actions. This is great for MBM because this tells us exactly when to act on messages from other players: at the end of the queue.

`bool HandlerBattle.instance.Queue(PlayAction playAction)`

This `Queue` method is `ActionQueue.Add` wrapped in some general checks (Is the player in a battle? Did the battle end already?). If the action was successfully queued, the method returns true. 

***New PlayActions***  
There are some vanilla `PlayAction`s that are useful for modders. Things like`ActionApplyStatus`, `ActionDraw`, and `ActionTrigger` can have their uses. In the context of multiplayer, these vanilla actions do not show the card that procced them (as it's on another board. This brings us to the first `PlayAction`.

`ActionDisplayCardAndSequence(CardData/string[], Func<Entity,IEnumerator>/PlayAction, float beforeDelay = 0, float afterDelay = 0)`*

This action is broken into 4 stages. It will (1) create the entity given either `CardData` or a card string, (2) move the entity to the bottom-left corner of the screen, (3) perform the `IEnumerator` or `PlayAction` provided (optionally based on number of frenzy stacks), and (4) destroy itself afterwards.

If you want to do something weird and specific, this would be what you want. If you are extremely meticulous, you can also extend the class and override any of the stages. The two ways you probably want to use them is to play the displayed entity or place it somewhere in the battle. These two variations are the next two `PlayAction`s. 

`ActionPlayOtherCard(string[] messages, Friend friend, Entity entity, CardContainer container)`
`ActionGainCardToBattle(CardData/string[], Location location = Hand, float beforeDelay = 0, float afterDelay = 0)`  

There is one last `PlayAction`. This one is used to send a copy of a card to be played on another board. You are not expected to use it because `HandlerBattle` uses it implicitly (see Mystic code in the Sync mod).

`ActionSendCardToPlay(string[] messages, Friend friend, Entity entity, CardContainer container)`*

---

## Exercise 3: Magical Mallet

This exercise involves creating a special Gearhammer named the "Magical Mallet". After this mallet hits a target, it will leave the game and show up in another person's game. They can then use it and send it to another player (hopefully you). Theoretically, the same mallet could be used from the first fight to the final battle, gaining attack every time it's been collectively used. Good luck! (Tip: If you want to choose another player instead of it being random, now would be a good time to look at `StatusEffectInstantMessage` class.)

---

The easiest implementation is to copy a normal Gearhammer and give it two new effects: consume and a custom ApplyXWhenDestroyed (specifically consumed) where the `effectToApply` is an `StatusEffectInstantMessage` (this is also an MBM class) or equivalent. In a structure like the GitHub tutorials, 
```C#
//Don't forget to make the ApplyXWhenDestroyed and attach that to the card.
assets.Add(new StatusEffectDataBuilder(this)
	.Create<StatusEffectInstantMessage>("Send To Someone Else")
	.SubscribeToAfterAllBuildEvent<StatusEffectInstantMessage>( data =>
	{
		data.handler = "MY_HANDLER";
		data.toWhom = StatusEffectInstantMessage.ToWhom.Select;
		data.includeSelf = false;
		data.parts = new string[] {"GEARHAMMER", "{1}"};//{1} will encode the target
	}	
```

Now, if our handler routine looks like the example in Chapter 1, this "GEARHAMMER" message will lead to the `DoSomething2` method. We could write that as the following.

```C#
public void DoSomething2(Friend f, string[] m)
{
	string[] cardStrings = HandlerSystem.DecodeMessage(m);
	PlayAction action = new ActionGainCardToBattle(cardStrings, ActionGainCardToBattle.location.draw);
	HandlerBattle.instance.Queue(action);
}
```

---

## Chapter 4: Miscellaneous

This is a loose collection of other stuff that may be useful.

***MultEvents***  
There are a couple of events that you can hook onto. They are all found in the `MultEvents` class.

`OnHandlerSystemEnabled()`  
`OnHandlerSystemDisabled()`  
Once you finalize the party, `OnHandlerSystemEnabled` will fire. If you disband, `OnHandlerSystemDisabled` will fire.

`OnBattleViewerOpen(Friend f)`  
`OnBattleViewerClose(Friend f)`  
`OnOtherCardPlayed(Friend f, Entity e)`  
`OnSentCardToPlay(Friend f, Entity e)`  
The first two are self-explanatory. The third event fires sometime within `ActionPlayOtherCard` and the last event fies sometime within `ActionSendCardToPlay`.

`OnBlessingSelected(BossRewardData.Data data)`*  
This final event is called when a boss reward is selected (there didn't seem to be any existing events that di this already). 

***MultTextManager***  
To display a message to another player, you can use the following method

`void MultTextManager.AddEntry(string s, float size, Color color, float duration)`

The default (boring) font size is 0.4f. After the `duration` time has passed, the message will slowly fade within the next five seconds.

`MultTextManager`'s other method allows you to display a small message beside a player's icon (just like how the `feedback` parameter of `SendMessage` works. It is shown below.

`void VisFeedback(Friend friend, string s = "Sending..."`
