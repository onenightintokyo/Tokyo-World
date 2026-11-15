// Tokyo World Expert 1.6 — true late-game recipes.
ServerEvents.recipes(event => {
  const gate = (output, rows, key) => {
    event.remove({ output: output })
    event.shaped(output, rows, key).id('tokyo_world:expert_16/' + output.replace(':', '_'))
  }
  if (Platform.isLoaded('mekanism') && Platform.isLoaded('ae2') && Platform.isLoaded('powah')) {
    gate('mekanism:digital_miner', ['ACA','ERE','ANA'],
      {A:'mekanism:alloy_atomic', C:'mekanism:ultimate_control_circuit',
       E:'mekanism:steel_casing', R:'ae2:controller', N:'minecraft:netherite_ingot'})
    gate('mekanism:ultimate_energy_cube', ['ACA','ERE','ANA'],
      {A:'mekanism:alloy_atomic', C:'mekanism:ultimate_control_circuit',
       E:'mekanism:elite_energy_cube', R:'powah:energy_cell_blazing', N:'minecraft:nether_star'})
    gate('ae2:quantum_ring', ['FUF','ENE','FUF'],
      {F:'ae2:fluix_crystal', U:'mekanism:ultimate_control_circuit',
       E:'mekanism:ingot_refined_obsidian', N:'minecraft:nether_star'})
  }
  if (Platform.isLoaded('industrialforegoing') && Platform.isLoaded('mekanism') && Platform.isLoaded('ae2')) {
    gate('industrialforegoing:mob_duplicator', ['PCP','FQF','PNP'],
      {P:'industrialforegoing:plastic',C:'mekanism:ultimate_control_circuit',
       F:'industrialforegoing:machine_frame_advanced',Q:'ae2:quantum_ring',N:'minecraft:nether_star'})
  }
  if (Platform.isLoaded('draconicevolution') && Platform.isLoaded('mekanism') && Platform.isLoaded('ae2')) {
    gate('draconicevolution:awakened_core', ['ADA','UQU','ANA'],
      {A:'draconicevolution:awakened_draconium_ingot',D:'draconicevolution:draconium_core',
       U:'mekanism:ultimate_control_circuit',Q:'ae2:quantum_ring',N:'minecraft:nether_star'})
  }
  if (Platform.isLoaded('mekanism') && Platform.isLoaded('powah')) {
    gate('mekanism:mekasuit_bodyarmor', ['AUA','CNC','AEA'],
      {A:'mekanism:alloy_atomic',U:'mekanism:ultimate_energy_cube',
       C:'mekanism:ultimate_control_circuit',N:'minecraft:nether_star',E:'minecraft:netherite_chestplate'})
    gate('mekanism:meka_tool', ['AUA','CNC','APA'],
      {A:'mekanism:alloy_atomic',U:'mekanism:ultimate_energy_cube',
       C:'mekanism:ultimate_control_circuit',N:'minecraft:nether_star',P:'minecraft:netherite_pickaxe'})
  }
  console.info('[Tokyo World] Expert 1.6 late-game cross-mod recipes loaded')
})
