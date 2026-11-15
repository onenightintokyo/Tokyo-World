// Tokyo World — Expert Fun 0.4. One file = one custom technology progression layer.
// Recipes are deliberately challenging but avoid expensive mass-production bottlenecks.
// Existing Metallurgic Infuser recipe lives in a separate Tokyo World startup script.
ServerEvents.recipes(event => {
  const shaped = (output, pattern, keys) => {
    event.remove({ output: output })
    event.shaped(output, pattern, keys)
  }

  // Industrial Foregoing enters after basic mechanical and electrical technology.
  shaped('industrialforegoing:machine_frame_pity',
    ['IAI', 'RCR', 'IAI'],
    { I: 'minecraft:iron_ingot', A: 'create:andesite_alloy',
      R: 'minecraft:redstone', C: 'minecraft:iron_block' })

  shaped('industrialforegoing:machine_frame_simple',
    ['SPS', 'PFP', 'SPS'],
    { S: 'mekanism:ingot_steel', P: 'industrialforegoing:plastic',
      F: 'industrialforegoing:machine_frame_pity' })

  // Better energy production needs control circuits and an energy buffer.
  shaped('powah:reactor_starter',
    ['ICI', 'ERE', 'ICI'],
    { I: 'minecraft:iron_ingot', C: 'mekanism:basic_control_circuit',
      E: 'powah:energy_cell_starter', R: 'minecraft:redstone_block' })

  // A logistical storage controller is not available on day one.
  shaped('sophisticatedstorage:controller',
    ['ICI', 'CRC', 'ICI'],
    { I: 'minecraft:iron_ingot', C: 'create:precision_mechanism',
      R: 'minecraft:redstone_block' })

  // Logic requires Create's more precise machinery.
  shaped('integrateddynamics:logic_programmer',
    ['IRI', 'CPC', 'IRI'],
    { I: 'minecraft:iron_ingot', R: 'minecraft:redstone',
      C: 'create:andesite_alloy', P: 'create:precision_mechanism' })

  // Build a space industry from pre-existing electrical machinery.
  shaped('ad_astra:nasa_workbench',
    ['SCS', 'PWP', 'SCS'],
    { S: 'ad_astra:steel_ingot', C: 'mekanism:advanced_control_circuit',
      P: 'ad_astra:steel_plate', W: 'minecraft:crafting_table' })

  // Power your bee industry using factory parts rather than only iron.
  shaped('productivebees:centrifuge',
    ['IGI', 'RCR', 'IGI'],
    { I: 'minecraft:iron_ingot', G: 'create:cogwheel',
      C: 'minecraft:cauldron', R: 'minecraft:redstone' })

  // Optional tech from the expansion module. Executed only when installed.
  if (Platform.isLoaded('ae2')) {
    shaped('ae2:controller',
      ['SCS', 'ECE', 'SCS'],
      { S: 'mekanism:ingot_steel', C: 'mekanism:advanced_control_circuit',
        E: 'minecraft:ender_eye' })
  }

  if (Platform.isLoaded('rftoolsbuilder')) {
    // No recipe changes yet: RFTools Builder is unlocked through dedicated quests.
  }


  // Expert Fun tier 2: make special production lines depend on one another.
  // Keep these late-game recipes gated; basic machines stay approachable.
  shaped('mekanism:digital_miner',
    ['EPE', 'CSC', 'EDE'],
    { E: 'mekanism:elite_control_circuit',
      P: 'create:precision_mechanism',
      C: 'mekanism:steel_casing',
      S: 'powah:energy_cell_basic',
      D: 'minecraft:diamond_pickaxe' })

  if (Platform.isLoaded('enderio')) {
    shaped('enderio:alloy_smelter',
      ['IFI', 'CPC', 'IFI'],
      { I: 'mekanism:ingot_steel',
        F: 'minecraft:furnace',
        C: 'enderio:void_chassis',
        P: 'create:precision_mechanism' })

    shaped('enderio:sag_mill',
      ['IGO', 'CPC', 'OGO'],
      { I: 'minecraft:iron_ingot',
        G: 'create:cogwheel',
        O: 'minecraft:obsidian',
        C: 'enderio:void_chassis',
        P: 'minecraft:piston' })
  }

  if (Platform.isLoaded('draconicevolution') && Platform.isLoaded('ae2')) {
    shaped('draconicevolution:crafting_core',
      ['DCD', 'APA', 'DCD'],
      { D: 'draconicevolution:draconium_ingot',
        C: 'mekanism:ultimate_control_circuit',
        A: 'draconicevolution:draconium_core',
        P: 'ae2:controller' })
  }

  console.info('[Tokyo World] Expert Fun recipes registered')
})
