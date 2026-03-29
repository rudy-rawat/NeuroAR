/**
 * seed.js — Run once to populate the anatomy-vectors collection.
 * Usage: node seed.js
 *
 * This generates real embeddings via HuggingFace and inserts them into MongoDB.
 * After running, your anatomy-vectors collection will have enough documents to
 * create the Atlas Vector Search index.
 */

import { MongoClient } from "mongodb";
import { MongoDBAtlasVectorSearch } from "@langchain/mongodb";
import { HuggingFaceInferenceEmbeddings } from "@langchain/community/embeddings/hf";
import dotenv from "dotenv";

dotenv.config();

const anatomyDocs = [
  // ─── Heart ────────────────────────────────────────────────────────────────
  {
    text: "The heart is a muscular organ that pumps blood throughout the body via the circulatory system. It has four chambers: the left atrium, right atrium, left ventricle, and right ventricle.",
    metadata: { organ: "heart", topic: "structure", level: "basic" },
  },
  {
    text: "The mitral valve (bicuspid valve) separates the left atrium and left ventricle. The tricuspid valve separates the right atrium and right ventricle.",
    metadata: { organ: "heart", topic: "valves", level: "intermediate" },
  },
  {
    text: "The sinoatrial (SA) node is the heart's natural pacemaker located in the right atrium. It generates electrical impulses that control the heart's rhythm.",
    metadata: { organ: "heart", topic: "electrical system", level: "advanced" },
  },
  {
    text: "Coronary arteries supply oxygenated blood to the heart muscle itself. The left and right coronary arteries branch from the aorta just above the aortic valve.",
    metadata: { organ: "heart", topic: "blood supply", level: "intermediate" },
  },

  // ─── Brain ────────────────────────────────────────────────────────────────
  {
    text: "The brain is divided into three main parts: the cerebrum, cerebellum, and brainstem. The cerebrum is the largest part and controls higher functions like thought and language.",
    metadata: { organ: "brain", topic: "structure", level: "basic" },
  },
  {
    text: "The cerebral cortex is the outer layer of the cerebrum. It is divided into four lobes: frontal, parietal, temporal, and occipital, each responsible for different functions.",
    metadata: {
      organ: "brain",
      topic: "cerebral cortex",
      level: "intermediate",
    },
  },
  {
    text: "Neurons communicate via synapses using neurotransmitters such as dopamine, serotonin, acetylcholine, and GABA. The synapse is the gap between two neurons.",
    metadata: { organ: "brain", topic: "neurotransmission", level: "advanced" },
  },

  // ─── Lungs ────────────────────────────────────────────────────────────────
  {
    text: "The lungs are two spongy organs in the chest responsible for gas exchange. The right lung has three lobes and the left lung has two lobes to make room for the heart.",
    metadata: { organ: "lungs", topic: "structure", level: "basic" },
  },
  {
    text: "The alveoli are tiny air sacs in the lungs where gas exchange occurs. Oxygen passes from the alveoli into the blood, and carbon dioxide moves from the blood into the alveoli.",
    metadata: { organ: "lungs", topic: "gas exchange", level: "intermediate" },
  },
  {
    text: "Surfactant is a substance produced by type II alveolar cells that reduces surface tension in alveoli, preventing them from collapsing during exhalation.",
    metadata: { organ: "lungs", topic: "surfactant", level: "advanced" },
  },

  // ─── Kidney ───────────────────────────────────────────────────────────────
  {
    text: "The kidneys are two bean-shaped organs that filter blood, remove waste products, and produce urine. Each kidney contains about one million tiny filtering units called nephrons.",
    metadata: { organ: "kidney", topic: "structure", level: "basic" },
  },
  {
    text: "The nephron consists of a glomerulus and a tubule. Blood is filtered in the glomerulus, and useful substances are reabsorbed back into the blood in the tubule.",
    metadata: { organ: "kidney", topic: "nephron", level: "intermediate" },
  },
  {
    text: "The kidneys regulate blood pressure by producing renin, which activates the renin-angiotensin-aldosterone system (RAAS) to control fluid balance and vascular resistance.",
    metadata: { organ: "kidney", topic: "blood pressure", level: "advanced" },
  },

  // ─── Liver ────────────────────────────────────────────────────────────────
  {
    text: "The liver is the largest internal organ and performs over 500 functions including detoxification, protein synthesis, and production of digestive chemicals such as bile.",
    metadata: { organ: "liver", topic: "structure", level: "basic" },
  },
  {
    text: "The liver produces bile stored in the gallbladder, which helps digest fats in the small intestine. It also metabolises carbohydrates, proteins, and lipids.",
    metadata: {
      organ: "liver",
      topic: "digestive function",
      level: "intermediate",
    },
  },

  // ─── Stomach ──────────────────────────────────────────────────────────────
  {
    text: "The stomach is a muscular organ that breaks down food using hydrochloric acid and digestive enzymes. It has four regions: cardia, fundus, body, and pylorus.",
    metadata: { organ: "stomach", topic: "structure", level: "basic" },
  },
  {
    text: "Pepsin is the main digestive enzyme in the stomach, produced as an inactive precursor called pepsinogen. It is activated by the acidic environment (pH 1.5–3.5).",
    metadata: { organ: "stomach", topic: "digestion", level: "intermediate" },
  },
];

async function seed() {
  const client = new MongoClient(process.env.MONGODB_URI);
  await client.connect();
  console.log("Connected to MongoDB.");

  const db = client.db(process.env.MONGODB_DB_NAME);
  const collection = db.collection(process.env.MONGODB_COLLECTION_VECTORS);

  // Clear existing seed data
  await collection.deleteMany({ "metadata.organ": { $exists: true } });
  console.log("Cleared old seed documents.");

  const embeddings = new HuggingFaceInferenceEmbeddings({
    apiKey: process.env.HUGGINGFACEHUB_API_TOKEN,
    model:
      process.env.HUGGINGFACE_EMBEDDING_MODEL ??
      "sentence-transformers/all-MiniLM-L6-v2",
  });

  const vectorStore = new MongoDBAtlasVectorSearch(embeddings, {
    collection,
    indexName: process.env.MONGODB_VECTOR_INDEX ?? "vector_index",
  });

  console.log(`Embedding and inserting ${anatomyDocs.length} documents...`);
  await vectorStore.addDocuments(
    anatomyDocs.map((d) => ({
      pageContent: d.text,
      metadata: d.metadata,
    })),
  );

  console.log("Done! Collection is ready for Atlas Vector Search index.");
  await client.close();
}

seed().catch((err) => {
  console.error(err);
  process.exit(1);
});
