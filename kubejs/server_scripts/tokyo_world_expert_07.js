// Tokyo World Expert v0.7
// Cross-mod late-game recipes. Activated only if all participating mods are loaded.
// Technical backbone is intentionally expensive, but ordinary early-game recipes remain playable.
ServerEvents.recipes(event => {
  const gate = (output, pattern, keys) => {
    event.remove({ output: output })
    event.shaped(output, pattern, keys).id('tokyo_world:expert_07/' + output.replace(':', '_'))
  }

  if (Platform.isLoaded('mekanism') && Platform.isLoaded('ae2') && Platform.isLoaded('enderio')) {
    // Remote machine network requires actual digital infrastructure.
    gate('mekanism:quantum_entangloporter',
      ['ECE', 'SQS', 'ECE'],
      {
        E: 'minecraft:ender_eye',
        C: 'mekanism:elite_control_circuit',
        S: 'mekanism:steel_casing',
        Q: 'ae2:controller'
      })
  }

  if (Platform.isLoaded('industrialforegoing') && Platform.isLoaded('mekanism')) {
    // The advanced frame is not a day-one machine upgrade.
    gate('industrialforegoing:machine_frame_advanced',
      ['PGP', 'CFC', 'PGP'],
      {
        P: 'industrialforegoing:plastic',
        G: 'minecraft:gold_ingot',
        C: 'mekanism:advanced_control_circuit',
        F: 'industrialforegoing:machine_frame_simple'
      })

  }

  if (Platform.isLoaded('powah') && Platform.isLoaded('mekanism') &&
      Platform.isLoaded('draconicevolution')) {
    // True endgame energy needs two energy systems and rare components.
    gate('powah:reactor_nitro',
      ['DCD', 'ERE', 'DCD'],
      {
        D: 'draconicevolution:draconium_ingot',
        C: 'mekanism:ultimate_control_circuit',
        E: 'mekanism:ultimate_energy_cube',
        R: 'powah:reactor_niotic'
      })
  }

  console.info('[Tokyo World] Expert v0.7 late-game cross-mod recipes loaded')
})
