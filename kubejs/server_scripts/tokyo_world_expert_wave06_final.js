// TOKYO WORLD — WAVE 06 — EXPERT FINAL
// Forge 1.20.1 / KubeJS 2001.x
// Single source of truth for this wave's recipes and hard-world settings.
// Do not duplicate these outputs in the older Tokyo World scripts.
(function () {
  var settingsPath = 'kubejs/config/tokyo_world_expert_final.json';
  function getSettings() {
    try {
      var cfg = JsonIO.read(settingsPath);
      if (cfg != null) return cfg;
    } catch (err) {
      console.warn('[Tokyo World] Cannot read Expert Final configuration: ' + err);
    }
    // Fail safe: always retain expert gameplay if the config has been lost.
    return {
      world: {
        hardDifficulty: true, naturalRegeneration: false,
        playersSleepingPercentage: 100, insomnia: true, entityCrammingLimit: 16
      },
      gameplay: {
        gateBackpacks: true, gateStorage: true, gateIndustrial: true,
        gateMagic: true, gateWeapons: true, gateSpace: true
      }
    };
  }

  // These server-side defaults are applied when a world starts. They do not
  // depend on the launcher, so LAN/hosted servers use the same difficulty.
  ServerEvents.loaded(function (event) {
    var rules = getSettings().world || {};
    var server = event.server;
    if (rules.hardDifficulty !== false) server.runCommandSilent('difficulty hard');
    server.runCommandSilent('gamerule naturalRegeneration ' +
      (rules.naturalRegeneration === true ? 'true' : 'false'));
    var sleep = Math.max(0, Math.min(100, Number(rules.playersSleepingPercentage) || 100));
    server.runCommandSilent('gamerule playersSleepingPercentage ' + Math.floor(sleep));
    server.runCommandSilent('gamerule doInsomnia ' + (rules.insomnia !== false ? 'true' : 'false'));
    var cram = Math.max(1, Math.min(32, Number(rules.entityCrammingLimit) || 16));
    server.runCommandSilent('gamerule maxEntityCramming ' + Math.floor(cram));
    console.info('[Tokyo World] Expert Final world rules loaded: HARD, natural regen ' +
      (rules.naturalRegeneration === true ? 'ON' : 'OFF'));
  });

  ServerEvents.recipes(function (event) {
    var switches = getSettings().gameplay || {};
    var success = 0;

    function gate(output, rows, key, mods) {
      if (mods && !mods.every(function (name) { return Platform.isLoaded(name); }))
        return;
      // Never inject a recipe for a mod or ingredient which is not present.
      if (!Item.exists(output)) {
        console.warn('[Tokyo World] Expert output unavailable: ' + output);
        return;
      }
      for (var symbol in key) {
        if (!Item.exists(key[symbol])) {
          console.warn('[Tokyo World] Expert recipe ' + output +
            ' waits for missing ingredient ' + key[symbol]);
          return;
        }
      }
      event.remove({ output: output });
      event.shaped(output, rows, key).id(
        'tokyo_world:expert_final_wave06/' + output.replace(':', '_')
      );
      success++;
    }

    if (switches.gateBackpacks !== false) {
      // Early travel requires material preparation, but remains achievable
      // without needing late-game tech for the first inventory upgrade.
      gate('sophisticatedbackpacks:backpack',
        ['LIL', 'SCS', 'LIL'],
        { L: 'minecraft:leather', I: 'minecraft:iron_ingot',
          S: 'minecraft:string', C: 'minecraft:chest' },
        ['sophisticatedbackpacks']);
      gate('sophisticatedbackpacks:iron_backpack',
        ['III', 'IBI', 'CPC'],
        { I: 'minecraft:iron_ingot', B: 'sophisticatedbackpacks:backpack',
          C: 'minecraft:copper_ingot', P: 'create:precision_mechanism' },
        ['sophisticatedbackpacks', 'create']);
      gate('sophisticatedbackpacks:gold_backpack',
        ['GGG', 'GIG', 'PCP'],
        { G: 'minecraft:gold_ingot', I: 'sophisticatedbackpacks:iron_backpack',
          P: 'create:precision_mechanism', C: 'mekanism:basic_control_circuit' },
        ['sophisticatedbackpacks', 'create', 'mekanism']);
      gate('sophisticatedbackpacks:diamond_backpack',
        ['DDD', 'DGD', 'ACA'],
        { D: 'minecraft:diamond', G: 'sophisticatedbackpacks:gold_backpack',
          A: 'ae2:controller', C: 'mekanism:advanced_control_circuit' },
        ['sophisticatedbackpacks', 'ae2', 'mekanism']);
      gate('sophisticatedbackpacks:netherite_backpack',
        ['NNN', 'NCN', 'AUA'],
        { N: 'minecraft:netherite_ingot', C: 'sophisticatedbackpacks:diamond_backpack',
          A: 'mekanism:alloy_atomic', U: 'mekanism:ultimate_control_circuit' },
        ['sophisticatedbackpacks', 'mekanism']);
    }

    if (switches.gateStorage !== false) {
      // Sophisticated Storage controller is mid-game networking, not a free
      // substitute for an entire Applied Energistics system.
      gate('sophisticatedstorage:controller',
        ['PAP', 'SCS', 'PAP'],
        { P: 'create:precision_mechanism', A: 'ae2:controller',
          S: 'minecraft:redstone_block', C: 'sophisticatedstorage:barrel' },
        ['sophisticatedstorage', 'create', 'ae2']);
    }

    if (switches.gateIndustrial !== false) {
      // Multi-block foundations should require a real steel production line.
      gate('immersiveengineering:heavy_engineering',
        ['SAS', 'LCL', 'SAS'],
        { S: 'mekanism:steel_casing', A: 'mekanism:advanced_control_circuit',
          L: 'immersiveengineering:light_engineering', C: 'minecraft:obsidian' },
        ['immersiveengineering', 'mekanism']);
      gate('immersiveengineering:capacitor_hv',
        ['CRC', 'EME', 'CRC'],
        { C: 'minecraft:copper_block', R: 'minecraft:redstone_block',
          E: 'immersiveengineering:capacitor_mv',
          M: 'mekanism:elite_control_circuit' },
        ['immersiveengineering', 'mekanism']);
    }

    if (switches.gateMagic !== false) {
      gate('hexcasting:spellbook',
        ['EGE', 'CPC', 'ADA'],
        { E: 'minecraft:ender_eye', G: 'minecraft:gold_block',
          C: 'hexcasting:cypher', P: 'minecraft:enchanted_book',
          A: 'minecraft:amethyst_shard', D: 'minecraft:diamond' },
        ['hexcasting']);
    }

    if (switches.gateWeapons !== false) {
      gate('simplyswords:diamond_katana',
        ['ADA', 'ACA', 'ASA'],
        { A: 'minecraft:amethyst_shard',
          D: 'minecraft:diamond', C: 'mekanism:basic_control_circuit',
          S: 'minecraft:diamond_sword' },
        ['simplyswords', 'mekanism']);
    }

    if (switches.gateSpace !== false) {
      // Space age requires IE's heavy steel, Mekanism electronics and Create.
      // Original NASA-workbench-specific rocket recipes remain intact.
      gate('ad_astra:nasa_workbench',
        ['SCS', 'PNP', 'SIS'],
        { S: 'ad_astra:steel_plate', C: 'mekanism:elite_control_circuit',
          P: 'create:precision_mechanism', N: 'immersiveengineering:heavy_engineering',
          I: 'minecraft:crafting_table' },
        ['ad_astra', 'mekanism', 'immersiveengineering', 'create']);
      gate('ad_astra:oxygen_distributor',
        ['SCS', 'OBO', 'STS'],
        { S: 'ad_astra:steel_plate', C: 'mekanism:advanced_control_circuit',
          O: 'ad_astra:oxygen_loader', B: 'minecraft:blast_furnace',
          T: 'mekanism:steel_casing' },
        ['ad_astra', 'mekanism']);
      gate('ad_astra:fuel_refinery',
        ['SAS', 'PCP', 'SBS'],
        { S: 'mekanism:steel_casing', A: 'create:precision_mechanism',
          P: 'ad_astra:steel_plate', C: 'mekanism:advanced_control_circuit',
          B: 'immersiveengineering:heavy_engineering' },
        ['ad_astra', 'mekanism', 'immersiveengineering', 'create']);
    }
    console.info('[Tokyo World] Expert Final Wave 06: ' + success +
      ' recipes installed; no duplicate KubeJS recipe outputs.');
  });
})();
