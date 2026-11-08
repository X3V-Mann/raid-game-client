export const CASSETTES = [
  {
    id: "cassette_1",
    code: "cassette_1",
    title: "Good Ending",
    artist: "Неизвестный",
    src: "/audio/cassette_1.mp3",
    icon: "/items/other/cassette.gif",
  },
  {
    id: "cassette_2",
    code: "cassette_2",
    title: "Dirge for the Planet",
    artist: "Неизвестный",
    src: "/audio/cassette_2.mp3",
    icon: "/items/other/cassette.gif",
  },
  {
    id: "cassette_3",
    code: "cassette_3",
    title: "Desolated",
    artist: "Неизвестный",
    src: "/audio/cassette_3.mp3",
    icon: "/items/other/cassette.gif",
  },
];

export const cassetteByCode = (code) => CASSETTES.find((c) => c.code === code) || null;
