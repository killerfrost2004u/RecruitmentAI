import sqlite3
import pandas as pd
import os
from audio_processing import AdvancedAudioProcessor

class TrainingDataCollector:
    def __init__(self, db_path):
        self.db_path = db_path
        self.audio_processor = AdvancedAudioProcessor()
    
    def collect_from_database(self):
        """Collect training data from SQLite database"""
        conn = sqlite3.connect(self.db_path)
        
        # Get candidates with voice notes and English levels
        query = """
        SELECT c.Id, c.FullName, c.VoiceNotePath, c.EnglishLevel, c.MatchedOffers
        FROM Candidates c 
        WHERE c.VoiceNotePath IS NOT NULL AND c.VoiceNotePath != ''
        AND c.EnglishLevel IS NOT NULL AND c.EnglishLevel != 'Pending'
        """
        
        df = pd.read_sql_query(query, conn)
        conn.close()
        
        return df
    
    def extract_features_for_training(self, output_csv='training_data.csv'):
        """Extract features for all voice notes and save to CSV"""
        candidates_df = self.collect_from_database()
        
        features_list = []
        for _, candidate in candidates_df.iterrows():
            try:
                if os.path.exists(candidate['VoiceNotePath']):
                    print(f"Processing {candidate['FullName']}...")
                    
                    features = self.audio_processor.extract_comprehensive_features(
                        candidate['VoiceNotePath']
                    )
                    
                    # Add metadata
                    features['candidate_id'] = candidate['Id']
                    features['cefr_level'] = candidate['EnglishLevel']
                    features['voice_note_path'] = candidate['VoiceNotePath']
                    
                    features_list.append(features)
                    
            except Exception as e:
                print(f"Error processing {candidate['FullName']}: {e}")
                continue
        
        # Create DataFrame and save
        features_df = pd.DataFrame(features_list)
        features_df.to_csv(output_csv, index=False)
        print(f"Saved {len(features_df)} samples to {output_csv}")
        
        return features_df