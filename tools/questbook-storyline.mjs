// Tokyo World narrative questbook — 36 original chapter files, 518 stable quest IDs.
// Run from repository root: node tools/questbook-storyline.mjs
import fs from "node:fs";
import path from "node:path";

const questRoot = "config/ftbquests/quests";
const dir = path.join(questRoot, "chapters");
const groups = [
  ["6","PIERWSZY ŚLAD","THE FIRST SPARK"],
  ["b","MECHANIZMY","MECHANISMS"],
  ["c","OGIEŃ I STAL","FIRE AND STEEL"],
  ["e","ENERGIA I SIECI","POWER AND NETWORKS"],
  ["a","ŻYWA FABRYKA","THE LIVING FACTORY"],
  ["d","SEKRETY MAGII","ARCANE SECRETS"],
  ["9","POZA HORYZONT","BEYOND THE HORIZON"],
  ["5","DZIEDZICTWO TOKYO","THE TOKYO LEGACY"]
];
// Slug | group | Polish chapter title | narrative introduction | final thought
const specs = [
 ["tokyo_start",1,"Pierwszy ślad","Nie ma jeszcze fabryki, tylko kawałek ziemi i pomysł. Wszystko zaczyna się od prostego warsztatu.","Masz już miejsce, do którego można wrócić po każdej wyprawie."],
 ["tokyo_farming",1,"Ziemia i zapasy","Nikt nie postawi wielkiej fabryki o pustym żołądku. Zacznij od gospodarstwa, które utrzyma całą ekipę.","Zbiory stały się pewnym zapleczem, a nie kwestią szczęścia."],
 ["tokyo_create",2,"Pierwsze obroty","Koła zębate wyglądają niepozornie. Dopiero połączone z napędem pokazują, do czego naprawdę są zdolne.","Zamiast nosić wszystko ręcznie, możesz zdać się na pracujące mechanizmy."],
 ["create_extended",2,"Linia, która nie śpi","Niewielki warsztat zaczyna przypominać halę. Spróbuj połączyć urządzenia, by nie potrzebowały ciebie przy każdym ruchu.","Jedna maszyna przekazuje pracę następnej. Tak zaczyna się prawdziwa automatyzacja."],
 ["mega_factory",2,"Hala Tokyo","Warsztat robi się za ciasny. Potrzebujesz przestrzeni, w której kilka procesów może pracować równocześnie.","Wielka fabryka nabiera własnego rytmu, a ty możesz myśleć o kolejnych projektach."],
 ["factory_logic",2,"Myślące maszyny","Samo uruchomienie urządzenia to za mało. Czas nauczyć całą instalację, kiedy powinna działać i gdzie skierować materiały.","Maszyny nie czekają już przy każdej decyzji na człowieka."],
 ["tokyo_mekanism",3,"Iskra elektryczności","Przekładnie mają swoje ograniczenia. Prąd otwiera technologie, o których zwykły warsztat może tylko pomarzyć.","Energia przestała być ciekawostką, a stała się częścią codziennej pracy."],
 ["mekanism_industry",3,"Rytm przemysłu","Sama kruszarka nie zbuduje potężnego zakładu. Najważniejsze jest to, co dzieje się między kolejnymi urządzeniami.","Procesy zaczynają się zazębiać i każda maszyna ma swoje miejsce."],
 ["immersive_industry",3,"Ciężar stali","Wielkie konstrukcje nie wybaczają prowizorki. Czas poznać stal, przewody i potężne urządzenia przemysłowe.","Huta staje się mocnym fundamentem dla kolejnych planów."],
 ["metallurgy",3,"Sekrety stopów","Nie każdy metal nadaje się do każdej maszyny. Czas zrozumieć, kiedy liczy się skład materiału, a nie jego ilość.","Zamiast przypadkowych sztabek masz materiały dobrane do zadania."],
 ["enderio_industry",3,"Od iskry do stopu","Zwykły piec nie przygotuje każdego materiału. W tej części fabryki trzeba cierpliwie łączyć zaawansowane składniki.","Potrafisz wytwarzać stopy, od których zależą najlepsze urządzenia."],
 ["chemical_superplant",3,"Reakcja łańcuchowa","Rudy kryją w sobie więcej, niż daje zwykłe przetopienie. Chemia pozwoli odzyskać to, co wcześniej trafiało na straty.","Laboratorium pracuje jak część fabryki, nie odizolowana ciekawostka."],
 ["steel_alloy_empire",3,"Imperium metalu","Najlepsze konstrukcje potrzebują materiałów, których nie znajdziesz w gotowej skrzyni. Zbuduj własne zaplecze hutnicze.","Jesteś przygotowany na maszyny wymagające naprawdę wyjątkowych stopów."],
 ["powah_energy",4,"Prąd pod kontrolą","Jedna maszyna nie potrzebuje wielkiej elektrowni. Cała baza już tak. Zacznij budować zasilanie z zapasem.","Sieć energetyczna zaczyna wyprzedzać potrzeby twojej fabryki."],
 ["integrated_logic",4,"Logika zamiast dźwigni","Nie musisz stać przy każdej maszynie. Naucz się przekazywać informację, zanim zbudujesz większy system.","Sygnał wykonuje za ciebie pracę, która kiedyś wymagała kilkunastu przełączników."],
 ["storage_network",4,"Koniec ze skrzyniami","Najważniejszy przedmiot zawsze jest w ostatniej skrzyni. Można to zmienić, jeśli magazyn zacznie działać jak system.","Teraz łatwiej odnaleźć materiały niż zgubić je między skrzyniami."],
 ["logistics_pro",4,"Droga materiału","Każdy surowiec ma swoją trasę. Dopóki nosisz go sam, twoja fabryka jest tylko dużym warsztatem.","Przedmioty znajdują drogę do odpowiednich maszyn bez twojej pomocy."],
 ["ae2_networks",4,"Pamięć kryształów","Magazyn może zapamiętać więcej, niż potrafisz policzyć. Zacznij od kryształów i naucz je współpracować.","Sieć ME pozwala myśleć o produkcji, nie o szukaniu przedmiotów."],
 ["megawatt_network",4,"Elektrownia dla miasta","Mały generator przestaje wystarczać, kiedy baza żyje także w nocy. Pomyśl o rezerwie mocy.","Możesz rozbudowywać zakład bez ciągłego strachu przed brakiem energii."],
 ["me_data_center",4,"Cyfrowe serce bazy","Prawdziwy magazyn powinien wiedzieć, czego potrzeba do następnej budowy. Tutaj zaczyna się inteligentna produkcja.","Masz własne centrum danych, które może przygotować pracę całego zakładu."],
 ["logistics_optimization",4,"Nic się nie marnuje","Najmocniejsza maszyna nie pomoże, jeśli surowiec utknie w rurze. Zajmij się miejscami, które dotąd blokowały produkcję.","System dowozi wszystko na czas, nawet gdy fabryka pracuje na pełnych obrotach."],
 ["industrial_farming",5,"Pola bez gospodarza","Maszyny mogą pracować również tam, gdzie wcześniej potrzebna była motyka. Zbuduj uprawy, które nie wymagają ciągłego doglądania.","Rolnictwo naprawdę nadąża za rozwojem fabryki."],
 ["mystical_advanced",5,"Nasiona niemożliwego","Niektóre plony dają więcej niż jedzenie. Magia roślin wymaga jednak cierpliwości i właściwego podłoża.","Twoje pola potrafią produkować materiały, których kiedyś trzeba było szukać godzinami."],
 ["bees_advanced",5,"Fabryka w ulu","Pszczoły nie znają grafiku pracownika, a jednak dostarczają niesamowite zasoby. Przygotuj im odpowiednie warunki.","Pasieka stała się częścią twojego systemu produkcji."],
 ["food_architecture",5,"Dom między maszynami","Złóż bazę, w której naprawdę warto mieszkać. Warsztat potrzebuje kuchni, schowków i miejsca na odpoczynek.","Dom jest gotowy, a wyprawy nie muszą zaczynać się od szukania jedzenia."],
 ["automation_agriculture",5,"Żniwa bez końca","Wielkie projekty potrzebują nie tylko metalu, lecz także regularnego zaopatrzenia. Zadbaj o samowystarczalność.","Gospodarstwo pracuje, nawet kiedy cała ekipa odkrywa nowe miejsca."],
 ["botania_magic",6,"Kwiaty pełne many","Nie każda energia płynie kablem. W tym świecie kwiaty potrafią zasilać własne urządzenia.","Mana zaczyna przypominać kolejną gałąź twojej technologii."],
 ["ars_magic",6,"Księga zaklęć","Pierwszy czar jest tylko obietnicą. Prawdziwa sztuka polega na zrozumieniu Source i własnych możliwości.","Księga przestała być zabawką, a stała się prawdziwym narzędziem."],
 ["mana_engineering",6,"Stal spotyka magię","Rytuał i maszyna nie muszą ze sobą walczyć. Spróbuj połączyć ich moc w jednym projekcie.","Znalezienie wspólnego języka dla magii i techniki otwiera nowe drogi."],
 ["space_industry",7,"Bilet poza atmosferę","Zwykły warsztat nie przetrwa w próżni. Zbuduj sprzęt, od którego będzie zależało twoje bezpieczeństwo.","Rakieta przestaje być marzeniem zapisanym na kartce."],
 ["caves_adventure",7,"Głębiej niż zwykle","Pod powierzchnią kryją się miejsca, w których nawet dobry sprzęt bywa niewystarczający. Przygotuj się na nieznane.","Wracasz z wyprawy bogatszy o rzeczy, których nie znajdziesz na powierzchni."],
 ["cataclysm_bosses",7,"Ci, których lepiej nie budzić","Wielkie ruiny rzadko są puste. Zanim wejdziesz do środka, pomyśl o ekipie i drodze odwrotu.","Znasz cenę każdego starcia z legendarnym przeciwnikiem."],
 ["orbital_outpost",7,"Dom wśród gwiazd","Sam lot w kosmos nie jest jeszcze zwycięstwem. Prawdziwy sukces to miejsce, gdzie można bezpiecznie zostać.","Pierwsza kosmiczna baza nie jest już tylko prowizorycznym obozem."],
 ["boss_hunting_contracts",7,"Polowanie na legendy","Niektóre trofea zdobywa się tylko dzięki dobremu przygotowaniu i współpracy. Podejmij ryzyko, ale nie lekceważ przeciwnika.","Masz dowód, że nawet największe legendy można pokonać."],
 ["draconic_endgame",8,"Moc smoków","Wszystkie wcześniejsze projekty prowadziły do materiałów, których zwykła technologia nie potrafi okiełznać.","Masz w rękach potęgę, której użycie wymaga rozsądku."],
 ["tokyo_ultimate_project",8,"Projekt Tokyo World","Fabryki, wyprawy, pola i magia muszą w końcu zadziałać jako jedna całość. To wspólne dzieło całej ekipy.","Zbudowaliście coś więcej niż najdroższą maszynę. Zbudowaliście własny świat."]
];
if(specs.length!==36)throw Error("36 chapters required");
const expected=[2,4,7,8,5,3,5,2];
for(let i=0;i<8;i++)if(specs.filter(s=>s[1]===i+1).length!==expected[i])throw Error("Category "+i+" wrong count");

const uid=(prefix,n)=>prefix+n.toString(16).toUpperCase().padStart(12,"0");
const questPattern=/^\t\t\{\r?\n\t\t\tid: "(A130[0-9A-F]{12})"[\s\S]*?^\t\t\}/gm;
const titlePattern=/^\t\t\ttitle: "((?:\\.|[^"\\])*)"/m;
const chapterIds=[];
const allIds=new Set();
const details=[];

function tree(n){
 const nodes=Array.from({length:n},()=>({parents:[],x:8,y:0,role:"branch"}));
 nodes[0].role="root";
 if(n===6 || n===8){
  const dep=n===6?[[],[0],[0],[1,2],[3],[4]]:[[],[0],[0],[1],[2],[3,4],[5],[6]];
  const xs=n===6?[8,1,15,8,8,8]:[8,1,15,1,15,8,8,8];
  const ys=n===6?[0,4,4,8,12,16]:[0,4,4,8,8,12,16,20];
  for(let i=1;i<n;i++)nodes[i]={parents:dep[i],x:xs[i],y:ys[i],role:"branch"};
  nodes[n===6?3:5].role="join";nodes[n-1].role="final";return nodes;
 }
 const mid=n<=12?5:n<=16?7:9;
 let row=1;
 for(let i=1;i<mid;i++){
   nodes[i]={parents:i<=2?[0]:[i-2],x:i%2===1?1:15,y:row*4,role:"branch"};
   if(i%2===0)row++;
 }
 nodes[mid]={parents:[mid-2,mid-1],x:8,y:row*4,role:"join"};row++;
 for(let i=mid+1;i<n-2;i++){
   nodes[i]={parents:i<=mid+2?[mid]:[i-2],x:(i-mid)%2===1?1:15,y:row*4,role:"branch"};
   if((i-mid)%2===0)row++;
 }
 nodes[n-2]={parents:[n-4,n-3],x:8,y:row*4,role:"join"};
 nodes[n-1]={parents:[n-2],x:8,y:(row+1)*4,role:"final"};
 return nodes;
}
const situations=[
 "Najbardziej irytują te problemy, które wychodzą dopiero podczas pracy. Teraz na stole leży temat ",
 "Fabryka rośnie szybciej niż plan na papierze. Następną sprawą do ogarnięcia jest ",
 "Dobry pomysł potrzebuje czegoś więcej niż kilku skrzyń z częściami. Pora zająć się ",
 "Zanim rozstawisz kolejne urządzenia, spójrz na to, czego wciąż brakuje. Tym razem chodzi o ",
 "Nie wszystko musi powstawać od razu. Na dziś najważniejszy będzie temat ",
 "Nie ma sensu przyspieszać, jeśli podstawy ciągle zawodzą. Wróć do sprawy ",
 "Każda maszyna jest tylko jednym ogniwem większej całości. Teraz liczy się ",
 "Właśnie takie rzeczy odróżniają działającą bazę od sterty maszyn. Najpierw rozwiąż ",
 "Niektórych odkryć łatwo nie docenić. Przyjrzyj się bliżej sprawie ",
 "Ten fragment pracy może wydawać się niepozorny, ale szybko okaże się potrzebny. Na tapecie jest "
];
const chapterVoices={
 1:["Warto od razu zadbać o zapas — pierwsza noc potrafi zaskoczyć.","Nie musisz się spieszyć; dobry warsztat oszczędzi czasu później.","Każda mała poprawka robi różnicę, kiedy nie masz jeszcze wielkiego zaplecza."],
 2:["Pomyśl, gdzie popłynie ruch, zanim ustawisz następny mechanizm.","Dobrze, żeby materiał nie zatrzymywał się w połowie drogi.","Jeśli cały układ ma pracować sam, każde połączenie musi mieć sens."],
 3:["W hucie błędy kosztują sporo paliwa i jeszcze więcej cierpliwości.","Zanim odpalisz wielką linię, przygotuj solidne komponenty.","Przy tej skali produkcji nawet mały przestój szybko daje się we znaki."],
 4:["Dobry system powinien wiedzieć, gdzie wysłać to, co właśnie wyprodukowałeś.","Zadbaj o zapas i drogę awaryjną, zanim podłączysz kolejną maszynę.","Skrzynie, kable i sterowniki muszą w końcu mówić jednym językiem."],
 5:["Najlepsze plony przychodzą wtedy, gdy pomyślisz o całym cyklu, nie tylko o zbiorach.","Gdy wyjedziesz na wyprawę, gospodarstwo nadal powinno sobie radzić.","Zadbaj o porządek w uprawach, żeby surowce nie ginęły między kolejnymi zbiorami."],
 6:["Nie traktuj magii jak zwykłego przepisu — jej działanie często wymaga cierpliwości.","Kiedy już zrozumiesz źródło tej mocy, spróbuj wykorzystać je w bazie.","Nawet najprostszy rytuał jest ciekawszy, gdy wiesz, po co go wykonujesz."],
 7:["Przygotuj się tak, żeby było czym wrócić, nie tylko z czym wyruszyć.","Nie wszystko, co świeci w ciemności, jest skarbem.","Dobra wyprawa zaczyna się dużo wcześniej niż przy wejściu do lochu."],
 8:["Tu każde wcześniejsze odkrycie może znaleźć nowe zastosowanie.","Nie budujesz już pojedynczej maszyny — składasz w całość cały dotychczasowy świat.","Wielkie projekty są łatwiejsze, kiedy każdy z ekipy ma w nich własne zadanie."]
};
function narrative(spec,names,node,i){
 const title="„"+names[i]+"”";
 if(node.role==="root")return spec[3]+" Zacznij od sprawy "+title+". Bez tego dalsze pomysły zostałyby tylko szkicem.";
 if(node.role==="join")return "Dwie drogi rozwoju wreszcie spotykają się w jednym miejscu. Tym razem chodzi o "+title+". Dopiero gdy obie części są gotowe, możesz przekonać się, jak pracują razem.";
 if(node.role==="final")return spec[4]+" Ostatni sprawdzian to "+title+". Niech będzie dowodem na to, że cała wcześniejsza praca miała sens.";
 const k=(i*7+spec[1])%situations.length;
 const voice=chapterVoices[spec[1]][(i+spec[1])%3];
 const prev=node.parents.length ? names[node.parents[0]] : "";
 const bridge=(i%4===0 && prev)?"Wcześniej udało ci się już zamknąć temat „"+prev+"”. ":"";
 return bridge+situations[k]+title+". "+voice;
}
const localized=new Map();
for(const spec of specs){
 const [slug,group,newTitle]=spec;
 const filename=path.join(dir,slug+".snbt");
 let s=fs.readFileSync(filename,"utf8");
 const chapterId=(s.match(/^\tid: "(A120[0-9A-F]{12})"/m)||[])[1];
 if(!chapterId)throw Error("Chapter ID missing "+slug);
 const matches=[...s.matchAll(questPattern)];
 const names=matches.map(m=>{
  const mt=m[0].match(titlePattern);
  if(!mt)throw Error("Quest title missing "+slug);
  return JSON.parse('"'+mt[1]+'"');
 });
 const ids=matches.map(m=>m[1]);
 if(matches.length<6 || matches.length>20)throw Error("Unexpected count "+slug+" "+matches.length);
 const graph=tree(matches.length);
 const changed=matches.map((m,i)=>{
  let b=m[0],node=graph[i];
  const desc=narrative(spec,names,node,i);
  const text=/^\t\t\tdescription: \[[^\r\n]*\]/m;
  if(!text.test(b))throw Error("Unsupported description syntax: "+slug);
  b=b.replace(text,()=>"\t\t\tdescription: "+JSON.stringify([desc]));
  for(const [key,v] of [["x",node.x.toFixed(1)+"d"],["y",node.y.toFixed(1)+"d"]]){
   const field=new RegExp("^\\t\\t\\t"+key+": [^\\r\\n]+","m");
   if(!field.test(b))throw Error("Missing coordinate "+slug);
   b=b.replace(field,()=>"\t\t\t"+key+": "+v);
  }
  if(i!==0){
   const value="\t\t\tdependencies: ["+node.parents.map(j=>'"'+ids[j]+'"').join(", ")+"]";
   if(/^\t\t\tdependencies: \[[^\r\n]*\]/m.test(b))
      b=b.replace(/^\t\t\tdependencies: \[[^\r\n]*\]/m,()=>value);
   else b=b.replace(/^\t\t\ticon: "[^\r\n]*"/m,match=>match+"\n"+value);
  }
  return b;
 });
 let offset=0;
 s=s.replace(questPattern,()=>changed[offset++]);
 if(offset!==matches.length)throw Error("Rewrite incomplete "+slug);
 const groupInfo=groups[group-1];
 const order=specs.filter(x=>x[1]===group).findIndex(x=>x[0]===slug);
 const replacements=[
  [/^\ttitle: "[^\r\n]*"/m,"\ttitle: "+JSON.stringify("&"+groupInfo[0]+"&l"+newTitle)],
  [/^\tgroup: "[A-F0-9]{16}"/m,'\tgroup: "'+uid("A110",group)+'"'],
  [/^\torder_index: \d+/m,"\torder_index: "+order]
 ];
 for(const [re,val] of replacements) {
  if(!re.test(s))throw Error("Missing header "+slug);
  s=s.replace(re,()=>val);
 }
 const afterIds=[...s.matchAll(/^\t\t\tid: "(A130[0-9A-F]{12})"/gm)].map(x=>x[1]);
 if(JSON.stringify(afterIds)!==JSON.stringify(ids))throw Error("IDs changed "+slug);
 afterIds.forEach(q=>{if(allIds.has(q))throw Error("Repeated quest ID "+q);allIds.add(q);});
 fs.writeFileSync(filename,s);
 localized.set(chapterId,{group,pl:newTitle});
 details.push({slug,count:ids.length,group});
}
if(allIds.size!==518)throw Error("Expected 518 unique quests, got "+allIds.size);
let edges=new Map();
for(const detail of details){
 const s=fs.readFileSync(path.join(dir,detail.slug+".snbt"),"utf8");
 for(const q of s.matchAll(questPattern)){
  const deps=(q[0].match(/^\t\t\tdependencies: \[([^\r\n]*)\]/m)||[])[1]||"";
  const arr=[...deps.matchAll(/"(A130[0-9A-F]{12})"/g)].map(x=>x[1]);
  for(const d of arr)if(!allIds.has(d))throw Error("Invalid dependency "+q[1]+" => "+d);
  edges.set(q[1],arr);
 }
}
const visited=new Set(),inStack=new Set();
function check(node){
 if(inStack.has(node))throw Error("Cycle in quest tree: "+node);
 if(visited.has(node))return;
 inStack.add(node);
 for(const parent of edges.get(node)||[])check(parent);
 inStack.delete(node);visited.add(node);
}
for(const q of allIds)check(q);
fs.writeFileSync(path.join(questRoot,"chapter_groups.snbt"),
 "{\n\tchapter_groups: [\n"+groups.map((g,i)=>"\t\t{\n\t\t\tid: \""+uid("A110",i+1)+"\"\n\t\t\ttitle: "+JSON.stringify("&"+g[0]+"&l"+g[1])+"\n\t\t}").join("\n")+"\n\t]\n}\n");
for(const [lang,labelIndex] of [["pl_pl",1],["en_us",2]]){
 const file=path.join(questRoot,"lang",lang+".snbt");
 let s=fs.readFileSync(file,"utf8");
 const oldEn=new Map([...s.matchAll(/^\tchapter\.(A120[0-9A-F]{12})\.title: "([^"]+)"/gm)].map(x=>[x[1],x[2].replace(/^&[0-9a-f]&l\d+\s+&r&f/,"")]));
 s=s.replace(/^\s*chapter_group\.A110[0-9A-F]{12}\.title: [^\r\n]*(?:\r?\n)?/gm,"");
 s=s.replace(/^\s*chapter\.A120[0-9A-F]{12}\.title: [^\r\n]*(?:\r?\n)?/gm,"");
 const rows=groups.map((g,i)=>"\tchapter_group."+uid("A110",i+1)+".title: "+JSON.stringify("&"+g[0]+"&l"+g[labelIndex]));
 for(const [chId,data] of localized){
   const g=groups[data.group-1];
   const name=lang==="pl_pl"?data.pl:(oldEn.get(chId)||data.pl);
   rows.push("\tchapter."+chId+".title: "+JSON.stringify("&"+g[0]+"&l"+name));
 }
 s=s.replace(/^\{\r?\n/,()=>"{\n"+rows.join("\n")+"\n");
 fs.writeFileSync(file,s);
}
const summary=[
 "# Tokyo World — questy jak opowieść",
 "",
 "Zmieniono **518 questów w 36 rozdziałach i 8 tematycznych kategoriach**.",
 "Questy mają prawdziwą strukturę: początek, rozwidlenie, ponowne połączenie, rozbudowę i finał.",
 "Zachowano identyfikatory questów, zadania materiałowe i nagrody. Zapis ukończenia nie powinien zostać wyzerowany.",
 "Usunięto automatyczne formuły typu ETAP X/16 i opisowe instrukcje o równoległych gałęziach.",
 "",
 "## Kategorie",
 ...groups.map((g,i)=>"- "+g[1]+": "+details.filter(x=>x.group===i+1).length+" rozdziałów"),
 "",
 "## Walidacja",
 "- Liczba questów: "+allIds.size,
 "- Unikalne ID: OK",
 "- Wszystkie zależności istnieją: OK",
 "- Brak cykli: OK",
 "- Pozostawiono nagrody i cele w oryginalnych blokach SNBT",
 "",
 "Po uruchomieniu sprawdź drzewka w FTB Quests, szczególnie pierwszy rozdział i finał Tokyo World."
].join("\n")+"\n";
fs.mkdirSync("docs",{recursive:true});
fs.writeFileSync("docs/QUESTBOOK_STORY_AUDIT.md",summary);
console.log("Tokyo World questbook rebuilt:",details.length,"chapters,",allIds.size,"quests and 8 groups.");
